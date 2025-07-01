using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcHealthRegeneration : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        float _outsideCombatRegenerationDelay;
        float _currentOutsideCombatRegen;
        bool _isUnconscious;
        
        static float OutsideCombatRegenerationDelay => GameConstants.Get.npcRegenerationDelay;
        static float OutsideCombatRegenerationPercent => GameConstants.Get.npcHealthRegenerationOutsideCombat;
        static float OutsideCombatRegenerationPercentUnconscious => GameConstants.Get.npcHealthRegenerationUnconscious;
        LimitedStat Health => ParentModel.Health;
        float MaxHealthValue => ParentModel.MaxHealth.ModifiedValue;
        float HealthRegenValue => ParentModel.Stat(AliveStatType.HealthRegen).ModifiedValue;

        protected override void OnFullyInitialized() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, () => _outsideCombatRegenerationDelay = OutsideCombatRegenerationDelay, this);
            ParentModel.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, this);
            ParentModel.ListenTo(UnconsciousElement.Events.LoseConscious, () => OnConsciousChanged(true), this);
            ParentModel.ListenTo(UnconsciousElement.Events.RegainConscious, () => OnConsciousChanged(false), this);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(Update);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(Update);
        }

        void OnStateChanged(Change<IState> change) {
            if (change.to is StateIdle) {
                _outsideCombatRegenerationDelay = OutsideCombatRegenerationDelay;
            }
        }
        
        void OnConsciousChanged(bool isUnconscious) {
            _currentOutsideCombatRegen = isUnconscious ? OutsideCombatRegenerationPercentUnconscious : OutsideCombatRegenerationPercent;
            _isUnconscious = isUnconscious;
        }

        void Update(float deltaTime) {
            var isInCombat = ParentModel.IsInCombat();
            if (_outsideCombatRegenerationDelay > 0 && !isInCombat) {
                _outsideCombatRegenerationDelay -= deltaTime;
            }

            if (!Health.IsMaxFloat) {
                bool outsideCombatRegen = _isUnconscious || (!isInCombat && _outsideCombatRegenerationDelay <= 0f);
                Regenerate(deltaTime, outsideCombatRegen);
            }
        }
        
        void Regenerate(float deltaTime, bool outsideCombatRegen) {
            float hpRegen = HealthRegenValue;
            if (outsideCombatRegen) {
                hpRegen += MaxHealthValue * _currentOutsideCombatRegen;
            }
            if (hpRegen > 0f) {
                hpRegen *= deltaTime;
                Health.IncreaseBy(hpRegen);
            }
        }
    }
}