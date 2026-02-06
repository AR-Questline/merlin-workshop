using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    /// <summary>
    /// Sets HP to 1 when Health Element registers killing blow.
    /// If it's a NpcElement it will add UnconsciousElement.
    /// </summary>
    public partial class KillPreventionElement : KillPreventionElement<Location>, IRefreshedByAttachment<KillPreventionAttachment> {
        public override ushort TypeForSerialization => SavedModels.KillPreventionElement;

        bool _disableUnconscious;
        
        protected override IAlive GetAlive => ParentModel.TryGetElement<IAlive>();
        protected override NpcElement TryGetNpc => ParentModel.TryGetElement<NpcElement>();
        protected override bool DisableUnconscious => _disableUnconscious;

        public void InitFromAttachment(KillPreventionAttachment spec, bool isRestored) {
            _disableUnconscious = spec.DisableUnconscious;
        }
    }
    
    public abstract partial class KillPreventionElement<T> : Element<T>, IKillPreventionListener where T : IModel {
        protected virtual IAlive GetAlive => ParentModel.TryGetElement<IAlive>();
        [CanBeNull] protected virtual NpcElement TryGetNpc => ParentModel.TryGetElement<NpcElement>();
        protected virtual bool DisableUnconscious => false;

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(Init);
        }

        protected override void OnRestore() {
            ParentModel.AfterFullyInitialized(Init);
        }

        protected virtual void Init() {
            IAlive alive = GetAlive;
            if (alive == null) {
                // this case happens when npc was killed bypassing kill prevention => ExecuteAction
                Discard();
                return;
            }
            KillPreventionDispatcher.RegisterListener(alive, this);
        }

        public virtual bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            float hpAfterDamage = healthElement.Health.ModifiedValue - damage.Amount;
            if (hpAfterDamage > 0f) {
                return false;
            }

            NpcElement npc = TryGetNpc;
            if (npc != null) {
                if (npc.Faction.IsHostileTo(damage.DamageDealerPure.Faction)) {
                    return false;
                }

                using var currentCrimeOwnersFor = npc.GetCurrentCrimeOwnersFor(CrimeArchetype.None);
                if (!currentCrimeOwnersFor.IsEmpty && CrimeUtils.HasCommittedUnforgivableCrime(currentCrimeOwnersFor.PrimaryOwner)) {
                    return false;
                }

                if (!DisableUnconscious) {
                    npc.AddMarkerElement<UnconsciousElement>();
                }
            }
            
            //Fake damage to trigger all VFX etc (can't deal precalculated damage because others modifiers can be applied) 
            Damage placeholderDamage = damage;
            placeholderDamage.RawData.SetToZero();
            DamageParameters placeholderDamageParameters = placeholderDamage.Parameters;
            placeholderDamageParameters.ForceDamage = 0; //Prevent Stagger
            placeholderDamageParameters.PoiseDamage = 0;
            placeholderDamage.Parameters = placeholderDamageParameters;
            
            healthElement.TakeDamage(placeholderDamage);
            healthElement.Health.SetTo(1f);

            return true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop || ParentModel.HasBeenDiscarded) {
                return;
            }

            IAlive alive = GetAlive;
            if (alive != null) {
                KillPreventionDispatcher.UnregisterListener(alive, this);
            }
        }
    }
}
