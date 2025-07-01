using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateAlertWander : NpcState<StateAlert> {
        const float MinDelay = 0.5f, MaxDelay = 2.5f;
        const float AlertDecreaseRate = 0.25f;
        const float ReachedDelay = 2f;
        readonly Observe _observe = new();
        Wander _wander;
        public bool Reached { get; private set; }
        bool _reachedWanderDestination;
        bool _isArcher, _isPreyAnimal, _updatedMovementStateAfterDelayElapsed;
        float _randomDelay, _inReachedStateDuration;
        Vector3? _currentAlertTarget;

        public bool IsTrotting { get; private set; }
        VelocityScheme VelocityScheme => AI.AlertStack.VelocityScheme;

        public override void Init() {
            _wander = new Wander(CharacterPlace.Default, VelocityScheme);
            _wander.OnEnd += OnReach;
        }

        protected override void OnEnter() {
            base.OnEnter();
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            //AI.InAlertWithWeapons = Parent.WalkWithWeapons;
            AI.AlertStack.TopDecreaseRate = AlertDecreaseRate;
            AI.AlertStack.AlertVisionGain = (int) AlertStack.AlertStrength.Max;
            _isArcher = Npc.Inventory.Items.Any(i => i.IsRanged);
            _isPreyAnimal = Npc.Template.IsPreyAnimal;
            _randomDelay = RandomUtil.UniformFloat(MinDelay, MaxDelay);
            _updatedMovementStateAfterDelayElapsed = false;
            ResetReached();
            UpdateMovementMainState();
            SetAlertDestination(true);
            IsTrotting = VelocityScheme == VelocityScheme.Trot;
            Npc.TryGetElement<BarkElement>()?.OnWanderSpeedChanged(this);
        }

        protected override void OnExit() {
            base.OnExit();
            ResetReached();
        }

        public override void Update(float deltaTime) {
            if (!_isPreyAnimal || _wander.PathComplete) {
                AI.AlertStack.TopDecreaseRate = AlertDecreaseRate;
            }

            // if (!AI.InAlertWithWeapons && AI.AlertValue > StateAlert.PatrolWithWeaponsPercentage) {
            //     AI.InAlertWithWeapons = true;
            // }

            if (_reachedWanderDestination) {
                _inReachedStateDuration += deltaTime;
                if (_inReachedStateDuration > ReachedDelay) {
                    Reached = true;
                    return;
                }
            }
            
            if (_randomDelay > 0) {
                _randomDelay -= deltaTime;
            } else if (!_updatedMovementStateAfterDelayElapsed) {
                SetAlertDestination(true);
                _updatedMovementStateAfterDelayElapsed = true;
            }

            SetAlertDestination();
            UpdateMovementMainState();
            VelocityScheme scheme = VelocityScheme;
            _wander.UpdateVelocityScheme(scheme);

            if (!IsTrotting && scheme == VelocityScheme.Trot) {
                IsTrotting = true;
                Npc.TryGetElement<BarkElement>()?.OnWanderSpeedChanged(this);
            } else if (IsTrotting && scheme == VelocityScheme.Walk) {
                IsTrotting = false;
                Npc.TryGetElement<BarkElement>()?.OnWanderSpeedChanged(this);
            }
        }

        void UpdateMovementMainState() {
            bool observe = _isArcher 
                           && AIUtils.CanSee(AI.Coords + Vector3.up, AI.AlertTarget + Vector3.up) 
                           && (AI.AlertTarget - AI.Coords).sqrMagnitude < AI.Data.perception.MaxDistanceSqr(AI);
            if (!observe && !_isPreyAnimal && _randomDelay <= 0) {
                if (Movement.CurrentState != _wander && !_reachedWanderDestination) {
                    Reached = false;
                    Movement.ChangeMainState(_wander);
                }
            } else if (Movement.CurrentState != _observe) {
                Movement.ChangeMainState(_observe);
            }
        }

        void SetAlertDestination(bool force = false) {
            if (!force && _currentAlertTarget.HasValue && _currentAlertTarget.Value.EqualsApproximately(AI.AlertTarget, 0.1f)) {
                return;
            }

            var alertTarget = AI.AlertTarget;
            _currentAlertTarget = alertTarget;
            ResetReached();
            var position = alertTarget;
            if (_isArcher) {
                position = AIUtils.FindBetterPositionForArcher(AI.VisionDetectionOrigin, position, 1).GetValueOrDefault(position);
            }
            _wander.UpdateDestination(position);
        }

        void OnReach() {
            _reachedWanderDestination = true;
        }

        void ResetReached() {
            _reachedWanderDestination = false;
            _inReachedStateDuration = 0;
            Reached = false;
        }
    }
}