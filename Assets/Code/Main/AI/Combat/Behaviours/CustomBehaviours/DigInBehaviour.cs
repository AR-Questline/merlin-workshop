using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class DigInBehaviour : CustomEnemyBehaviour<EnemyBaseClass>, IMinDistanceToTargetProvider, IKillPreventionListener {
        const float PositionUpdateInterval = 0.5f;
        
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 10;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField, Range(0, 20f)] float minDistanceToTarget = 5f;
        [SerializeField, Range(0, 100f)] float maxDistanceToTarget = 15f;
        [SerializeField, RichEnumExtends(typeof(VelocityScheme))] RichEnumReference velocityScheme = VelocityScheme.Run;
        [SerializeField] NpcStateType digInStateType;
        [SerializeField] NpcStateType digOutStateType;
        [SerializeField] FloatRange digOutAtDistance = new FloatRange(3, 5);
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference digInVFX;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference undergroundVFX;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference digOutVFX;

        NoMove _noMove;
        KeepPosition _keepPosition;
        Vector3 _randomPos;
        RVOLayer _originalObstacleLayer;
        float _originalSlowDownTime;
        float _timeSinceLastPositionUpdate;
        bool _digOut;
        bool _digIn;
        bool _preventFinalBlow;
        IPooledInstance _undergroundVFXInstance;
        StatTweak _movementSpeedTweak;
        CharacterPlace _currentDesiredPosition;
        CancellationTokenSource _undergroundVfxToken;

        public float MinDistanceToTarget => 0;
        public override int Weight => weight;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => digInStateType;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;

        protected override void OnInitialize() {
            _noMove = new NoMove();
            _keepPosition = new KeepPosition(new CharacterPlace(ParentModel.Coords, 0.5f), velocityScheme.EnumAs<VelocityScheme>(), 0f);
            KillPreventionDispatcher.RegisterListener(Npc, this);
        }

        protected override bool OnStart() {
            _digIn = false;
            Npc.NpcAI.SetActivePerceptionUpdate(false);
            DistancesToTargetHandler.AddMinDistanceToTargetProvider(ParentModel.NpcElement, this);
            _movementSpeedTweak = StatTweak.Multi(ParentModel.NpcElement.CharacterStats.MovementSpeedMultiplier, 0.75f, parentModel: this);
            ParentModel.NpcElement.Controller.AlivePrefab.SetActive(false);

            _randomPos = Random.insideUnitCircle.normalized.ToHorizontal3() * digOutAtDistance.RandomPick();
            _currentDesiredPosition = DesiredPosition();
            _keepPosition.UpdatePlace(_currentDesiredPosition);
            
            ParentModel.NpcMovement.InterruptState(_noMove);
            ParentModel.NpcElement.Controller.RichAI.maxSpeed = 0;
            _digOut = false;
            return true;
        }

        public override void Update(float deltaTime) {
            _timeSinceLastPositionUpdate += deltaTime;

            if (_timeSinceLastPositionUpdate >= PositionUpdateInterval) {
                _currentDesiredPosition = DesiredPosition();
                _keepPosition.UpdatePlace(_currentDesiredPosition);
            }

            if (!_digIn) {
                return;
            }
            
            if (!_digOut && _currentDesiredPosition.Contains(ParentModel.Coords)) {
                DigOut();
            }

            if (_digOut && NpcGeneralFSM.CurrentAnimatorState.Type != digOutStateType) {
                ParentModel.StartWaitBehaviour();
            }
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart && digInVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(digInVFX, ParentModel.Coords, Quaternion.identity).Forget();
                return;
            }

            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                DigIn();
                ParentModel.NpcMovement.InterruptState(_keepPosition);
            }
        }

        public override bool UseConditionsEnsured() {
            var distanceToTarget = ParentModel.DistanceToTarget;
            return distanceToTarget >= minDistanceToTarget && distanceToTarget <= maxDistanceToTarget;
        }

        void DigIn() {
            _digIn = true;
            _preventFinalBlow = true;
            
            DiscardMovementSpeedMultiplier();
            _movementSpeedTweak = StatTweak.Multi(ParentModel.NpcElement.CharacterStats.MovementSpeedMultiplier, 2, parentModel: this);
            
            var rvoController = ParentModel.NpcElement.Controller.RvoController;
            _originalObstacleLayer = rvoController.collidesWith;
            rvoController.collidesWith = 0;
            ParentModel.NpcElement.SpawnedVisualPrefab.SetActive(false);

            RichAI richAI = ParentModel.NpcElement.Controller.RichAI;
            _originalSlowDownTime = richAI.slowdownTime;
            richAI.slowdownTime = 0;
            
            SpawnUndergroundVFX().Forget();
        }

        async UniTaskVoid SpawnUndergroundVFX() {
            if (undergroundVFX.IsSet) {
                _undergroundVfxToken?.Cancel();
                _undergroundVfxToken = new CancellationTokenSource();
                _undergroundVFXInstance = await PrefabPool.Instantiate(undergroundVFX, Vector3.zero,
                    Quaternion.identity, ParentModel.ParentModel.MainView.transform,
                    cancellationToken: _undergroundVfxToken.Token);
            }
        }

        void DigOut() {
            DiscardUndergroundVFX();

            if (digOutVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(digInVFX, ParentModel.Coords, Quaternion.identity).Forget();
            }

            var npcElement = ParentModel.NpcElement;
            npcElement.SpawnedVisualPrefab.SetActive(true);
            var controller = ParentModel.NpcElement.Controller;
            controller.ResetTargetRootRotation();
            controller.SetForwardInstant((npcElement.GetCurrentTarget().Coords - ParentModel.Coords).ToHorizontal2());
            npcElement.Movement.InterruptState(_noMove);
            DiscardMovementSpeedMultiplier();
            _movementSpeedTweak = StatTweak.Multi(npcElement.CharacterStats.MovementSpeedMultiplier, 0.9f, parentModel: this);
            RestoreRichAISettings();
            ParentModel.SetAnimatorState(digOutStateType);
            _digOut = true;
            AfterDigOut().Forget();
        }

        void DiscardUndergroundVFX() {
            _undergroundVfxToken?.Cancel();
            _undergroundVfxToken = null;
            
            if (_undergroundVFXInstance != null) {
                VFXUtils.StopVfxAndReturn(_undergroundVFXInstance, 1f);
                _undergroundVFXInstance = null;
            }
        }

        void RestoreRichAISettings() {
            RichAI richAI = ParentModel.NpcElement.Controller.RichAI;
            richAI.maxSpeed = 0;
            richAI.slowdownTime = _originalSlowDownTime;
        }

        CharacterPlace DesiredPosition() {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            if (target == null) {
                return new CharacterPlace(ParentModel.Coords, 0.5f);
            }

            var nodeInfo = AstarPath.active.GetNearest(_randomPos + target.Coords);
            if (nodeInfo.node == null) {
                return new CharacterPlace(ParentModel.Coords, 0.5f);
            }

            return new CharacterPlace(nodeInfo.position, 0.5f);
        }

        protected override void BehaviourExit() {
            Npc.NpcAI.SetActivePerceptionUpdate(true);
            DistancesToTargetHandler.RemoveMinDistanceToTargetProvider(ParentModel.NpcElement, this);
            DiscardMovementSpeedMultiplier();
            ParentModel.NpcElement.Controller.AlivePrefab.SetActive(true);

            ParentModel.NpcElement.Controller.RvoController.collidesWith = _originalObstacleLayer;
            ParentModel.NpcMovement.StopInterrupting();

            if (_digIn) {
                DiscardUndergroundVFX();
                DiscardMovementSpeedMultiplier();
                ParentModel.NpcElement.SpawnedVisualPrefab.SetActive(true);
                AfterDigOut().Forget();
                _digOut = true;
            }
        }
        
        // === Helpers
        async UniTaskVoid AfterDigOut() {
            if (await AsyncUtil.DelayTime(this, 0.25f)) {
                _preventFinalBlow = false;
            }
        }
        
        void DiscardMovementSpeedMultiplier() {
            _movementSpeedTweak?.Discard();
            _movementSpeedTweak = null;
        }
        
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        public bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            if (!_preventFinalBlow) {
                return false;
            }
            
            float hpAfterDamage = healthElement.Health.ModifiedValue - damage.Amount;
            if (hpAfterDamage > 0f) {
                return false;
            }
            
            healthElement.Health.SetTo(1f);
            return true;
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && ParentModel.ParentModel.TryGetElement(out NpcElement npc) && !npc.HasBeenDiscarded) {
                KillPreventionDispatcher.UnregisterListener(npc, this);
            }
        }
    }
}