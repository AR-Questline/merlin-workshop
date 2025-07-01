using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateAlertLookAt : NpcState<StateAlert> {
        // LookAt Animation Type
        const float FasterAnimationVisibilityThreshold = 0.33f;
        // Lerp Alert Value - Time Based
        const float MaxAlertTime = 5f;
        const float MinAlert = (int) AlertStack.AlertStrength.Medium;
        const float MaxAlert = (int) AlertStack.AlertStrength.Max;
        // Lerp Alert Value - Alert Stack Based
        const float SlowerAlertThreshold = 25f;
        const float SlowerAlertGainModifier = 0.5f;

        readonly NoMove _noMove = new();
        readonly Observe _observe = new();
        float _inStateTime;
        bool _inState;
        bool _canEnterAnimation;
        
        protected override void OnEnter() {
            base.OnEnter();
            _inState = false;
            //AI.InAlertWithWeapons = false;
            AI.AlertStack.AlertVisionGain = MinAlert * SlowerAlertGainModifier;
            _inStateTime = 0f;
            LateInitialize().Forget();
        }
        
        async UniTaskVoid LateInitialize() {
            if (!await AsyncUtil.DelayFrame(Npc)) {
                return;
            }
            if (Npc.TryGetElement<SimpleInteractionExitMarker>(out var marker)) {
                // We can't add new models on after discarded event
                marker.ListenToLimited(Model.Events.AfterDiscarded, () => _canEnterAnimation = true, Npc);
            } else {
                _canEnterAnimation = true;
            }
        }

        public override void Update(float deltaTime) {
            if (!_inState) {
                if (_canEnterAnimation && !Npc.IsEquippingWeapons) {
                    EnterAnimation();
                    _canEnterAnimation = false;
                }
                return;
            }
            UpdateMovementMainState();
            
            _inStateTime += deltaTime;
            float alertVisionGain = Mathf.Lerp(MinAlert, MaxAlert, _inStateTime / MaxAlertTime);
            if (AI.AlertStack.AlertValue < SlowerAlertThreshold) {
                alertVisionGain *= SlowerAlertGainModifier;
            }
            AI.AlertStack.AlertVisionGain = alertVisionGain;
        }
        
        void UpdateMovementMainState() {
            if (!AI.ObserveAlertTarget) {
                if (Movement.CurrentState != _noMove) {
                    Movement.ChangeMainState(_noMove);
                }
            } else if (Movement.CurrentState != _observe) {
                Movement.ChangeMainState(_observe);
            }
        }

        void EnterAnimation() {
            if (Npc is not { HasBeenDiscarded: false, NpcAI: { InAlert: true } }) {
                return;
            }
            NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(Npc, 0, new TimeDuration(0.5f));
            bool useFasterAnimation = AI.MaxHeroVisibilityGain > FasterAnimationVisibilityThreshold;
            if (useFasterAnimation) {
                _inStateTime += MaxAlertTime / 2f;
                Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.AlertStartQuick);
            } else {
                Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.AlertStart);
            }
            _inState = true;
        }
    }
}