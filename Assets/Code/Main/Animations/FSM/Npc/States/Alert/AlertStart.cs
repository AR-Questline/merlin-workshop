using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Alert {
    public partial class AlertStart : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AlertStart;

        public const float StartingAngularSpeedMultiplier = 0.1f;
        public const float StartingAngularSpeedMultiplierDuration = 2f;
        public const float AngularSpeedMultiplier = 0.3f;
        public const float AngularSpeedMultiplierDuration = 1f;

        float _angularTimeCounter;
        bool _angularSpeedApplied;
        
        public override NpcStateType Type => NpcStateType.AlertStart;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(Npc, StartingAngularSpeedMultiplier, new TimeDuration(StartingAngularSpeedMultiplierDuration));
            if (RemainingDuration < 10f) {
                // Don't stop transitioning if using fallback animation
                Npc.NpcAI.AlertStack.AlertTransitionsPaused = true;
            }
            Npc.NpcAI.ObserveAlertTarget = true;
        }

        protected override void OnExit(bool restarted) {
            Npc.NpcAI.AlertStack.AlertTransitionsPaused = false;
        }

        protected override void OnUpdate(float deltaTime) {
            _angularTimeCounter += deltaTime;
            if (!_angularSpeedApplied && _angularTimeCounter > StartingAngularSpeedMultiplierDuration || _angularSpeedApplied && _angularTimeCounter > AngularSpeedMultiplierDuration) {
                NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(Npc, AngularSpeedMultiplier, new TimeDuration(AngularSpeedMultiplierDuration));
                _angularSpeedApplied = true;
                _angularTimeCounter = 0f;
            }
            
            if (RemainingDuration <= 0.15f) {
                ParentModel.SetCurrentState(AlertLookAt.GetStartingAlertState(Npc));
            }
        }
    }
}