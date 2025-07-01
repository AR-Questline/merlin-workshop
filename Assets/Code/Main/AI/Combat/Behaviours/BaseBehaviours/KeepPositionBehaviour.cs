using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class KeepPositionBehaviour : EnemyBehaviourBase, IBehaviourBase {
        public const float TargetPositionAcceptRange = 0.25f;
        const float UpdatePositionAfter = 0.5f;

        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 15;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField, ShowIf(nameof(CanChangeBehaviourWhenInCorrectPosition))] float minimumTimeInStateToStartNewBehaviour = 1.5f;
        [SerializeField, RichEnumExtends(typeof(VelocityScheme))] RichEnumReference chaseVelocityScheme = VelocityScheme.Run;
        [SerializeField] protected float maxStrafeDistance = KeepPosition.DefaultMaxStrafeDistance;
        [SerializeField] protected bool changeVelocityOnDistance;
        [SerializeField, RichEnumExtends(typeof(VelocityScheme)), ShowIf(nameof(changeVelocityOnDistance))] RichEnumReference closeVelocityScheme = VelocityScheme.Trot;
        [SerializeField, ShowIf(nameof(changeVelocityOnDistance))] protected float distanceToChangeVelocity = 6f;
        [SerializeField, ShowIf(nameof(changeVelocityOnDistance))] protected bool invertRotationSchemes;
        [SerializeField] bool preventLeavingInTurn = true;

        public override int Weight => weight;
        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;
        protected virtual bool CanChangeBehaviourWhenInCorrectPosition => true;
        protected virtual float MinimumTimeInStateToStartNewBehaviour => minimumTimeInStateToStartNewBehaviour;
        protected virtual bool InCorrectPosition => _targetPosition.Contains(ParentModel.Coords);
        protected VelocityScheme CloseVelocity => closeVelocityScheme.EnumAs<VelocityScheme>();
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        protected VelocityScheme ChaseVelocity => chaseVelocityScheme.EnumAs<VelocityScheme>();

        protected float _inStateDuration, _lastPositionUpdate;
        protected KeepPosition _keepPosition;
        protected CharacterPlace _targetPosition;
        
        protected override bool StartBehaviour() {
            if (ParentModel.TryGetCombatSlotPosition(out var combatSlotPosition)) {
                ParentModel.SetDesiredPosition(combatSlotPosition);
            }
            _targetPosition = GetTargetPosition();
            if (InCorrectPosition && 
                CanChangeBehaviourWhenInCorrectPosition && 
                ParentModel.TryToStartNewBehaviourExcept(this, ParentModel.TryGetElement<ShieldManKeepPositionBehaviour>())) {
                return false;
            }

            CreateMovementState();
            
            ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            ParentModel.SetAnimatorState(NpcStateType.Idle);
            
            _keepPosition.VelocityChanged += VelocitySchemeChanged;
            VelocitySchemeChanged(CloseVelocity);
            
            return true;
        }

        protected virtual void CreateMovementState() {
            if (changeVelocityOnDistance) {
                _keepPosition = new KeepPosition(_targetPosition, CloseVelocity, maxStrafeDistance, distanceToChangeVelocity, ChaseVelocity, invertRotationSchemes);
            } else {
                _keepPosition = new KeepPosition(_targetPosition, ChaseVelocity, maxStrafeDistance);
            }
        }
        
        public override void Update(float deltaTime) {
            _inStateDuration += deltaTime;
            VerifyTargetPosition(deltaTime);
            
            if (ParentModel.NpcMovement.CurrentState != _keepPosition) {
                ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            }
            
            TryToLeaveBehaviour();
        }

        public override bool UseConditionsEnsured() => true;
        
        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_keepPosition);
            _inStateDuration = 0;
            _keepPosition.VelocityChanged -= VelocitySchemeChanged;
            _keepPosition = null;
        }
        
        protected virtual void VelocitySchemeChanged(VelocityScheme velocityScheme) { }

        void TryToLeaveBehaviour() {
            if (preventLeavingInTurn && NpcGeneralFSM.CurrentAnimatorState.Type == NpcStateType.TurnMovement) {
                return;
            }

            if (_inStateDuration > MinimumTimeInStateToStartNewBehaviour || (InCorrectPosition && CanChangeBehaviourWhenInCorrectPosition)) {
                ParentModel.TryToStartNewBehaviourExcept(this, ParentModel.TryGetElement<ShieldManKeepPositionBehaviour>());
            }
        }
        
        // === Helpers
        void VerifyTargetPosition(float deltaTime) {
            if (!ParentModel.RequiresCombatSlot) {
                _targetPosition = GetTargetPosition();
                _keepPosition.UpdatePlace(_targetPosition);
                return;
            }
            
            _lastPositionUpdate += deltaTime;
            if (_lastPositionUpdate > UpdatePositionAfter) {
                _targetPosition = GetTargetPosition();
                if (!InCorrectPosition) {
                    _keepPosition.UpdatePlace(_targetPosition);
                }
                _lastPositionUpdate = 0;
            }
        }

        protected virtual CharacterPlace GetTargetPosition() {
            return GetTargetPosition(ParentModel);
        }

        public static CharacterPlace GetTargetPosition(EnemyBaseClass enemyBaseClass, float positionAcceptRange = TargetPositionAcceptRange) {
            var npc = enemyBaseClass.NpcElement;
            var target = npc?.GetCurrentTarget();
            return GetTargetPosition(enemyBaseClass, npc, target, positionAcceptRange);
        }

        public static CharacterPlace GetTargetPosition(EnemyBaseClass enemyBaseClass, NpcElement npc, ICharacter target, float positionAcceptRange = TargetPositionAcceptRange) {
            if (target == Hero.Current) {
                return new CharacterPlace(enemyBaseClass.DesiredPosition, positionAcceptRange);
            }
            var desiredDistance = DistancesToTargetHandler.DesiredDistanceToTarget(npc, target);
            return CombatBehaviourUtils.GetTargetPosition(enemyBaseClass, target, desiredDistance, positionAcceptRange);
        }
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<KeepPositionBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] { NpcStateType.CombatIdle, NpcStateType.CombatMovement };

            // === Constructor
            public Editor_Accessor(KeepPositionBehaviour behaviour) : base(behaviour) { }
        }
    }
}
