using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    /// <summary>
    /// NPC exits combat as Unconscious, will regain conscious after Hero ended its combat (and some delay).
    /// </summary>
    public partial class UnconsciousElement : Element<NpcElement>, ICanMoveProvider {
        public override ushort TypeForSerialization => SavedModels.UnconsciousElement;

        const float RecoveryDuration = 10f;

        protected int _dangerCounter;
        
        public bool CanMove => false;
        public virtual bool AddKillUnconsciousAction => true;
        public bool IsUnconscious { get; private set; } = true;

        public new static class Events {
            public static readonly Event<NpcElement, UnconsciousElement> LoseConscious = new(nameof(LoseConscious));
            public static readonly Event<NpcElement, UnconsciousElement> RegainConscious = new(nameof(RegainConscious));
            public static readonly Event<NpcElement, UnconsciousElement> UnconsciousKilled = new(nameof(UnconsciousKilled));
        }
        
        protected override void OnInitialize() {
            IsUnconscious = true;
            NpcCanMoveHandler.AddCanMoveProvider(ParentModel, this);
            ParentModel.ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        protected override void OnRestore() {
            IsUnconscious = true;
            NpcCanMoveHandler.AddCanMoveProvider(ParentModel, this);
            ParentModel.ParentModel.OnVisualLoaded(_ => OnVisualLoadedFromRestore().Forget());
        }

        async UniTaskVoid OnVisualLoadedFromRestore() {
            //It need to wait for both EnemyBaseClass and RagdollDeathBehaviour being really initialized
            //EnemyBaseClass is initialized when all their elements are added (On Visual Loaded)
            //RagdollDeathBehaviour is initialized when ragdoll is cached (On Visual Loaded as well)
            if (await AsyncUtil.DelayFrame(this, 1)) {
                InitializeLoseConscious();
            }
        }

        void OnVisualLoaded(Transform parentTransform) {
            InitializeLoseConscious();
        }

        void InitializeLoseConscious() {
            LoseConscious();
            InitRegainConsciousListeners();
        }

        protected virtual void InitRegainConsciousListeners() {
            if (Hero.Current.HeroCombat.IsHeroInFight) {
                _dangerCounter++;
                Hero.Current.ListenTo(ICharacter.Events.CombatExited, OnHeroCombatExited, this);
            } 
            
            if (ParentModel.NpcChunk is { Data: { HasDanger: true } }) {
                _dangerCounter++;
                WaitForChunkSafety(ParentModel.NpcChunk).Forget();
            } 
            
            if (_dangerCounter == 0) {
                DelayRegainConscious().Forget();
            }
        }

        void LoseConscious() {
            ParentModel.Trigger(Events.LoseConscious, this);
            ParentModel.NpcAI.ExitCombat(true, true);
            ParentModel.AddElement(new UnconsciousInvisibility(this));
            if (AddKillUnconsciousAction) {
                ParentModel.ParentModel.AddElement(new KillUnconsciousAction(this));
            }
            ParentModel.CloseEyes();
            StatTweak.Override(ParentModel.NpcStats.Sight, 0, TweakPriority.Override, this);
            StatTweak.Override(ParentModel.NpcStats.Hearing, 0, TweakPriority.Override, this);
            ParentModel.Element<NpcCrimeReactions>().SetSeeingHero(false, true);
            ParentModel.Statuses.RemoveAllNegativeStatuses();
        }

        void OnHeroCombatExited() {
            OnAnyDangerEnded();
        }
        
        async UniTaskVoid WaitForChunkSafety(NpcChunk chunk) {
            if (await AsyncUtil.WaitUntil(this, () => !chunk.Data.HasDanger)) { 
                OnChunkDangerEnded();
            }
        }

        void OnChunkDangerEnded() {
            OnAnyDangerEnded();
        }

        protected virtual void OnAnyDangerEnded() {
            _dangerCounter--;
            if (_dangerCounter <= 0) {
                DelayRegainConscious().Forget();
            }
        }

        async UniTaskVoid DelayRegainConscious() {
            if (await AsyncUtil.DelayTime(this, RecoveryDuration)) {
                RegainConscious();
            }
        }

        public void RegainConscious() {
            IsUnconscious = false;
            ParentModel.Trigger(Events.RegainConscious, this);
            ParentModel.NpcAI.AlertStack.Reset();
            ParentModel.OpenEyes();
            ParentModel.Statuses.RemoveAllNegativeStatuses();
            Discard();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            NpcCanMoveHandler.RemoveCanMoveProvider(ParentModel, this);
        }
    }
}
