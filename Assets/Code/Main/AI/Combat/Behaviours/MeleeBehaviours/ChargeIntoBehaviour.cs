using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class ChargeIntoBehaviour : KnockBackAttackBehaviour {
        [SerializeField] bool canBeInterrupted;
        [SerializeField] NpcStateType animatorPrepareStateType = NpcStateType.ChargeEnter;
        [SerializeField] NpcStateType animatorEndStateType = NpcStateType.ChargeExit;
        [SerializeField, ShowIf(nameof(canBeInterrupted))] NpcStateType animatorInterruptedStateType = NpcStateType.ChargeInterrupt;
        [SerializeField] float chargeSpeed = 10f;
        [SerializeField] AnimationCurve chargeSpeedCurve;
        [SerializeField] float chargeDistanceOverflow = 2f;
        [SerializeField] ARAnimationEvent animationEvent;
        [SerializeField] AoeActivationType aoeActivationType = AoeActivationType.EnemyHit;
        
        public override bool CanBeUsed => true;
        protected override NpcStateType StateType => _preparingCharge ? animatorPrepareStateType : animatorStateType;
        protected override MovementState OverrideMovementState => new NoMove();
        
        bool _preparingCharge;
        RaycastCheck _raycastCheck;
        IHandOwner<ICharacter> _handOwner;
        Vector3 _chargeDirection;
        float _remainingDistance;
        float _inChargeDuration;
        bool _isExiting;
        Vector3 _coordsBefore;
        IEventListener _onDamageDealt;
        IEventListener _beforeDamageTaken;

        protected override void OnInitialize() {
            _raycastCheck = new RaycastCheck {
                prevent = GameConstants.Get.obstaclesMask
            };
            base.OnInitialize();
        }

        protected override bool OnStart() {
            ParentModel.SetAnimatorState(animatorPrepareStateType);
            _inChargeDuration = 0f;
            _preparingCharge = true;
            _isExiting = false;
            _handOwner ??= ParentModel.NpcElement.GetHandOwner();
            
            return true;
        }

        protected override bool StartBehaviour() {
            if (canBeInterrupted) {
                _beforeDamageTaken = ParentModel.NpcElement.HealthElement.ListenTo(HealthElement.Events.BeforeDamageTaken, () => Exit(animatorInterruptedStateType), this);
            }
            
            _onDamageDealt = ParentModel.NpcElement.ListenToLimited(HealthElement.Events.OnDamageDealt, OnChargeDamageDealt, this);
            CalculateDestination();
            return base.StartBehaviour();
        }
        
        void OnChargeDamageDealt() {
            if (_isExiting) {
                return;
            }
            if (aoeActivationType is AoeActivationType.EnemyHit) {
                SpawnDamageSphere();
            }
            Exit(animatorInterruptedStateType);
        }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            bool isSpecialAttack = animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger;
            bool canSpawnAoeOnSpecialAttack = aoeActivationType is AoeActivationType.SpecialAttackEvent;
            
            if (isSpecialAttack && canSpawnAoeOnSpecialAttack) {
                SpawnDamageSphere();
            }
        }
        
        void CalculateDestination() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            _chargeDirection = (target.Coords - ParentModel.Coords).ToHorizontal3().normalized;
            _remainingDistance = ParentModel.DistanceToTarget + chargeDistanceOverflow;
            ParentModel.NpcMovement.Controller.SetForwardInstant(_chargeDirection.ToVector2());
        }

        protected override void AfterStart() {
            _handOwner.OnAttackRelease(animationEvent.CreateData());
        }
        
        public override void OnUpdate(float deltaTime) {
            if (_isExiting) {
                var currentAnimatorStateType = ParentModel.NpcElement.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM).CurrentAnimatorState.Type;
                if (currentAnimatorStateType != animatorEndStateType && currentAnimatorStateType != animatorInterruptedStateType) {
                    base.OnAnimatorExitDesiredState();
                }
                
                return;
            }
            
            if (_preparingCharge) {
                CalculateDestination();
            } else if (deltaTime > 0) {
                UpdateChargeMovement(deltaTime);
            }
        }

        void UpdateChargeMovement(float deltaTime) {
            _inChargeDuration += deltaTime;
            
            var curveSpeed = chargeSpeedCurve.Evaluate(_inChargeDuration);
            var stepDistance = chargeSpeed * deltaTime * curveSpeed;
            var deltaVector = _chargeDirection * stepDistance;
            
            Npc.Controller.Move(deltaVector);
            _remainingDistance -= stepDistance;
            
            bool chargeBlocked = _raycastCheck.Raycast(ParentModel.Coords + Vector3.up, _chargeDirection, _remainingDistance, 0).Prevented;
            
            if (chargeBlocked || _remainingDistance <= 0f) {
                Exit(animatorEndStateType);
            }
        }
        
        void Exit(NpcStateType exitState) {
            ParentModel.SetAnimatorState(exitState);
            _isExiting = true;
        }

        protected override void BehaviourExit() {
            World.EventSystem.TryDisposeListener(ref _beforeDamageTaken);
            World.EventSystem.TryDisposeListener(ref _onDamageDealt);
            ParentModel.NpcElement.RemoveElementsOfType<NpcAngularSpeedMultiplier>();
            _handOwner.OnAttackRecovery(animationEvent.CreateData());
            ParentModel.NpcElement.Movement.StopInterrupting();
            base.BehaviourExit();
        }
        
        protected override void OnAnimatorExitDesiredState() {
            _preparingCharge = false;
        }

        enum AoeActivationType : byte {
            [UnityEngine.Scripting.Preserve] Never,
            EnemyHit,
            SpecialAttackEvent,
        }
    }
}
