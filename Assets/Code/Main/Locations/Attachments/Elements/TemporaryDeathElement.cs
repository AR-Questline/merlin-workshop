using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TemporaryDeathElement : Element<Location>, IRefreshedByAttachment<TemporaryDeathAttachment>, IKillPreventionListener, IWithDuration {
        public override ushort TypeForSerialization => SavedModels.TemporaryDeathElement;

        const int AfterCombatDelayTime = 10;
        
        TemporaryDeathAttachment _spec;
        [Saved] bool _fakeDeath;
        
        public bool RestoredWhileDead { get; private set; }

        public new static class Events {
            public static readonly Event<TemporaryDeathElement, bool> TemporaryDeathStateChanged = new(nameof(TemporaryDeathStateChanged));
        }

        public void InitFromAttachment(TemporaryDeathAttachment spec, bool isRestored) {
            _spec = spec;
            RestoredWhileDead = isRestored && _fakeDeath;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
            KillPreventionDispatcher.RegisterListener(ParentModel.TryGetElement<IAlive>(), this);
        }

        void OnVisualLoaded(Transform parentTransform) {
            if (_fakeDeath) {
                if (TryGetElement<TimeDuration>(out var duration)) {
                    duration.ListenTo(Model.Events.AfterDiscarded, _ => WaitToResurrect(0).Forget(), this);
                } else {
                    WaitToResurrect(1).Forget();
                }
            }
        }

        public bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            if (_fakeDeath) {
                return true;
            }
            
            float hpAfterDamage = healthElement.Health.ModifiedValue - damage.Amount;
            if (hpAfterDamage > 0f) {
                return false;
            }
            
            //Fake damage to trigger all VFX etc (can't deal precalculated damage because others modifiers can be applied) 
            Damage placeholderDamage = damage; 
            placeholderDamage.RawData.SetToZero();
            DamageParameters placeholderDamageParameters = placeholderDamage.Parameters;
            placeholderDamageParameters.ForceDamage = 0; //Prevent Stagger
            placeholderDamage.Parameters = placeholderDamageParameters;
            
            healthElement.TakeDamage(placeholderDamage);
            healthElement.Health.SetTo(1f);
            
            FakeDeath(damage.DamageDealer is Hero).Forget();
            return true;
        }

        async UniTaskVoid FakeDeath(bool killedByHero) {
            if (!ParentModel.TryGetElement<NpcElement>(out var npc)) {
                return;
            }

            // We might be currently entering combat. we need to skip the call
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }

            if (_spec.StoryToRunOnTemporaryDeath is {IsValid: true}) {
                StoryUtils.TryStartLocationStory(_spec.StoryToRunOnTemporaryDeath, ParentModel);
            }
            
            AddElement(new TimeDuration(killedByHero ? _spec.DeathDurationHero : _spec.DeathDurationOther))
                .ListenTo(Model.Events.AfterDiscarded, _ => WaitToResurrect(0).Forget(), this);

            npc.NpcAI.UpdateHeroVisibility(0, true);
            npc.NpcAI.ForceStopCombatWithHero();
            npc.Interactor.Stop(InteractionStopReason.StoppedIdlingInstant, false);
            npc.Behaviours.AddOverride(new InteractionFakeDeathFinder(npc.Coords, _spec.DeathStartDuration, _spec.DeathEndDuration, _spec.Animations, _spec.ForceChangeIntoGhost, _spec.IfForcedStayInGhost), null);

            if (_spec.DeathVFX is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(_spec.DeathVFX, npc.Coords, npc.Rotation).Forget();
            }
            
            _fakeDeath = true;
            this.Trigger(Events.TemporaryDeathStateChanged, true);
        }

        async UniTaskVoid WaitToResurrect(int delay = AfterCombatDelayTime) {
            if (!await AsyncUtil.DelayTime(this, delay)) {
                return;
            }
            if (Hero.Current.IsInCombat()) {
                Hero.Current.ListenTo(ICharacter.Events.CombatExited, () => WaitToResurrect().Forget(), ParentModel);
                return;
            }
            
            Resurrect();
        }

        void Resurrect() {
            if (_fakeDeath) {
                ParentModel.TryGetElement<NpcElement>()?.Health.SetToFull();
                this.Trigger(Events.TemporaryDeathStateChanged, false);
                Resurrecting().Forget();
            }
        }

        async UniTaskVoid Resurrecting() {
            if (!await AsyncUtil.DelayTime(this, _spec.DeathEndDuration)) {
                return;
            }
            _fakeDeath = false;
            RestoredWhileDead = false;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                return;
            }
            Resurrect();
            if (ParentModel is { HasBeenDiscarded: false }) {
                KillPreventionDispatcher.UnregisterListener(ParentModel.TryGetElement<IAlive>(), this);
            }
        }

        public IModel TimeModel => ParentModel;
    }
}
