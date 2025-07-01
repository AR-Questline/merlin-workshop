using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public abstract partial class SpellCastingBehaviourBase : CombatEnemyBehaviourBase, IBehaviourBase {
        protected const string BaseCastingGroup = "BaseCasting";
        protected const string SpellEffectGroup = "SpellEffect";
        protected const string SpellEffectVisualsGroup = "SpellEffectVisuals";
        // === Serialized Fields
        [SerializeField] bool canDashBackwards = true;
        [SerializeField, ShowIf(nameof(canDashBackwards))] float dashBackwardsBonusDistance = 3f;
        [BoxGroup(BaseCastingGroup), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        protected ShareableARAssetReference fireBallInHandPrefab;
        [SerializeField, ShowIf(nameof(InHandPrefabSet))] bool parentFireBallToHand = true;
        [SerializeField, ShowIf(nameof(InHandPrefabSet))] bool manuallyHandleInHandPrefabLifetime = true;
        [SerializeField, HideIf(nameof(manuallyHandleInHandPrefabLifetime))] float inHandPrefabLifetime = 5f;
        [SerializeField] NpcStateType animatorStateType = NpcStateType.MagicProjectile;
        [SerializeField] bool canCastOnceInRow = true;
        [BoxGroup(BaseCastingGroup), SerializeField, HideIf(nameof(useAdditionalHand))] protected CastingHand castingHand = CastingHand.OffHand;
        [BoxGroup(BaseCastingGroup), SerializeField] protected bool useAdditionalHand;
        [BoxGroup(BaseCastingGroup), SerializeField, ShowIf(nameof(useAdditionalHand))] protected AdditionalHand additionalHand;
        [BoxGroup(BaseCastingGroup), SerializeField] protected Vector3 castingPointOffset = Vector3.zero;
        [SerializeField] protected bool rotateToTarget = true;
        [BoxGroup(BaseCastingGroup), SerializeField] bool useHandSlotOrientation = false;
        [BoxGroup(BaseCastingGroup), SerializeField, ShowIf(nameof(useHandSlotOrientation))] Vector3 handSlotOrientationLocalDirection = Vector3.up;

        // === Properties & Fields
        public override bool CanBeUsed => !IsMuted && (ParentModel.HasUnreachablePathToHeroFromCombatSlotCondition() ||
                                                       CastsInRowCondition);
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool CanBeAggressive => true;
        protected virtual bool IsInValidState => _isDashing || NpcGeneralFSM.CurrentAnimatorState.Type == animatorStateType;
        protected virtual ShareableARAssetReference InHandPrefab => fireBallInHandPrefab;
        bool CastsInRowCondition => !canCastOnceInRow || _canBeUsedAgain;
        public override float MinDistance => canDashBackwards ? base.MinDistance - dashBackwardsBonusDistance : base.MinDistance;
        bool InHandPrefabSet => fireBallInHandPrefab is { IsSet: true };
        
        bool _canBeUsedAgain = true;
        bool _isDashing;
        CancellationTokenSource _cancellationToken;
        protected IPooledInstance _fireBallInstance;
        protected MovementState _overrideMovementState;

        protected override void OnInitialize() {
            ParentModel.NpcElement.ListenTo(NpcAnimatorSubstateMachine.Events.NpcDashBackEnded, OnDashEnded, this);
            ParentModel.ListenTo(EnemyBaseClass.Events.BehaviourStarted, OnBehaviourStarted, this);
        }

        // === Callbacks
        void OnDashEnded(NpcAnimatorState _) {
            if (ParentModel.CurrentBehaviour.Get() == this) {
                ParentModel.SetAnimatorState(animatorStateType);
                _isDashing = false;
            }
        }

        void OnBehaviourStarted(IBehaviourBase combatBehaviour) {
            if (combatBehaviour != this && combatBehaviour is AttackBehaviour) {
                _canBeUsedAgain = true;
            }
        }

        // === Lifecycle
        protected override bool StartBehaviour() {
            _canBeUsedAgain = false;
            _isDashing = canDashBackwards && ParentModel.DistanceToTarget < base.MinDistance;
            ParentModel.SetAnimatorState(_isDashing ? NpcStateType.DashBack : animatorStateType);
            _overrideMovementState = ParentModel.NpcMovement.ChangeMainState(GetDesiredMovementState());
            return true;
        }
        
        protected virtual MovementState GetDesiredMovementState() {
            return rotateToTarget ? new NoMoveAndRotateTowardsTarget() : new NoMove();
        }

        public override void Update(float deltaTime) {
            if (!IsInValidState) {
                ParentModel.StartWaitBehaviour();
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_overrideMovementState);
            ReturnInstantiatedPrefabs();
        }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                SpawnFireBallInHand().Forget();
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                CastSpell();
            }
        }

        public override void BehaviourInterrupted() {
            ReturnInstantiatedPrefabs();
        }

        // === Helpers
        protected virtual async UniTask SpawnFireBallInHand() {
            if (InHandPrefab is not { IsSet: true }) {
                return;
            }
            
            Transform hand = GetHand();
            _cancellationToken = new CancellationTokenSource();
            Vector3 spawnPoint = parentFireBallToHand ? castingPointOffset : castingPointOffset + hand.position;
            Quaternion rotation = parentFireBallToHand ? Quaternion.identity : hand.rotation;
            Transform parent = parentFireBallToHand ? hand : null;
            
            if (manuallyHandleInHandPrefabLifetime) {
                _fireBallInstance = await PrefabPool.Instantiate(InHandPrefab, spawnPoint, rotation, parent, cancellationToken: _cancellationToken.Token);
            } else {
                _fireBallInstance = await PrefabPool.InstantiateAndReturn(InHandPrefab, spawnPoint, rotation, inHandPrefabLifetime, parent, cancellationToken: _cancellationToken.Token);
            }
            
            if (HasBeenDiscarded) return;
            PlaySpecialAttackBeginAudio();
        }

        protected abstract UniTask CastSpell(bool returnFireballInHandAfterSpawned = true);

        protected virtual Transform GetHand() {
            if (useAdditionalHand) {
                var hand = ParentModel.GetAdditionalHand(additionalHand);
                if (hand != null) {
                    return hand;
                }
                Log.Minor?.Error($"{Npc.Name} has no additional hand at ID {additionalHand}");
                return Npc.MainHand;
            } else {
                return castingHand == CastingHand.MainHand ? Npc.MainHand : Npc.OffHand;
            }
        }
        
        protected Vector3 GetSpellUpDirection() {
            if (useHandSlotOrientation) {
                return GetHand().TransformDirection(handSlotOrientationLocalDirection);
            } 
            return Vector3.up;
        }
        
        protected virtual Vector3 GetSpellPosition() {
            if (_fireBallInstance?.Instance != null) {
                return _fireBallInstance.Instance.transform.position;
            } else {
                Transform hand = GetHand();
                return hand.position + hand.rotation * castingPointOffset;
            }
        }

        protected void PlaySpecialAttackBeginAudio() {
            Npc.PlayAudioClip(AliveAudioType.SpecialBegin.RetrieveFrom(Npc), true, GetFMODParameters());
        }

        protected void PlaySpecialAttackReleaseAudio() {
            Npc.PlayAudioClip(AliveAudioType.SpecialRelease.RetrieveFrom(ParentModel.NpcElement), true, GetFMODParameters());
        }

        FMODParameter[] GetFMODParameters() {
            return new FMODParameter[] { new("Index", SpecialAttackIndex) };
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            ReturnInstantiatedPrefabs();
        }
        
        // === Helpers
        protected virtual void ReturnInstantiatedPrefabs() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            if (manuallyHandleInHandPrefabLifetime) {
                _fireBallInstance?.Return();
                _fireBallInstance = null;
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<SpellCastingBehaviourBase> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Behaviour.animatorStateType.Yield();

            // === Constructor
            public Editor_Accessor(SpellCastingBehaviourBase behaviour) : base(behaviour) { }
        }
    }
}