using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public abstract partial class UseItemBehaviour : EnemyBehaviourBase, IBehaviourBase {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 5;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField, FoldoutGroup("Invoke Conditions")] float minDistanceToTarget = 3.5f;
        [SerializeField, FoldoutGroup("Asset References"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference itemInHand;
        [SerializeField, FoldoutGroup("Asset References"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference onUseVFX;
        
        [SerializeField, FoldoutGroup("Effect")]
        SkillReference skillEffectReference;

        [SerializeField, FoldoutGroup("Animation Settings")]
        bool overrideHand;
        [SerializeField, FoldoutGroup("Animation Settings"), ShowIf(nameof(overrideHand))]
        CastingHand castingHand = CastingHand.MainHand;
        [SerializeField, FoldoutGroup("Animation Settings"), ShowIf(nameof(overrideHand))]
        bool overrideAnimatorState;
        [SerializeField, FoldoutGroup("Animation Settings"), ShowIf(nameof(OverrideState))]
        NpcStateType animatorState = NpcStateType.UseItemMainHand;
        
        public override int Weight => weight;
        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => false;
        protected abstract bool CanBeUsed { get; }
        protected bool UseSkill => skillEffectReference.IsSet;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        bool OverrideState => overrideHand && overrideAnimatorState;

        bool _restoreMainHandItems;
        bool _restoreOffHandItems;
        HashSet<GameObject> _mainHandActiveChildren;
        HashSet<GameObject> _offHandActiveChildren;
        NoMove _noMove;
        CancellationTokenSource _cancellationToken;
        IPooledInstance _itemPrefab;
        NpcStateType _animatorStateType;

        protected override void OnInitialize() {
            base.OnInitialize();
            _noMove = new NoMove();
            _mainHandActiveChildren = new HashSet<GameObject>();
            _offHandActiveChildren = new HashSet<GameObject>();
        }

        protected override bool StartBehaviour() {
            ParentModel.NpcMovement.ChangeMainState(_noMove);
            SpawnItemInHand().Forget();
            return true;
        }

        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != _animatorStateType) {
                ParentModel.StartWaitBehaviour();
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_noMove);
            ReturnItemPrefab();
            
            if (_restoreMainHandItems) {
                RestoreMainHandItems();
            }

            if (_restoreOffHandItems) {
                RestoreOffHandItems();
            }
        }

        public override void BehaviourInterrupted() {
            ReturnItemPrefab();
            
            if (_restoreMainHandItems) {
                RestoreMainHandItems();
            }

            if (_restoreOffHandItems) {
                RestoreOffHandItems();
            }
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                UseItem();
            }
        }

        public override bool UseConditionsEnsured() => CanBeUsed && ParentModel.DistanceToTarget >= minDistanceToTarget;

        async UniTaskVoid SpawnItemInHand() {
            Transform hand;
            if (overrideHand) {
                if (overrideAnimatorState) {
                    _animatorStateType = animatorState;
                }
                if (castingHand == CastingHand.MainHand) {
                    UseMainHand(true);
                } else {
                    UseOffHand(true);
                }
            } else if (ParentModel.NpcElement.MainHand.childCount <= 0) {
                UseMainHand(false);
            } else if (ParentModel.NpcElement.OffHand.childCount <= 0) {
                UseOffHand(false);
            } else {
                UseMainHand(true);
            }
            
            void UseMainHand(bool hideItems) {
                hand = ParentModel.NpcElement.MainHand;
                _animatorStateType = NpcStateType.UseItemMainHand;
                if (hideItems) {
                    HideItemsInMainHand();
                }
            }

            void UseOffHand(bool hideItems) {
                hand = ParentModel.NpcElement.OffHand;
                _animatorStateType = NpcStateType.UseItemOffHand;
                if (hideItems) {
                    HideItemsInOffHand();
                }
            }

            ParentModel.SetAnimatorState(_animatorStateType);
            _cancellationToken = new CancellationTokenSource();
            _itemPrefab = await PrefabPool.Instantiate(itemInHand, Vector3.zero, Quaternion.identity, hand, cancellationToken: _cancellationToken.Token);
        }

        protected virtual void UseItem() {
            if (skillEffectReference.IsSet) {
                Skill skill = skillEffectReference.CreateSkill();
                ParentModel.NpcElement.Skills.LearnSkill(skill);
                skill.Submit();
                skill.Discard();
                SpawnVFX();
            }
        }

        protected void SpawnVFX(float duration = 3) {
            PrefabPool.InstantiateAndReturn(onUseVFX, Vector3.zero, Quaternion.identity, duration, ParentModel.DefaultVFXParent).Forget();
        }

        void HideItemsInMainHand() {
            _restoreMainHandItems = true;
            AIUtils.HideItemsInHand(ParentModel.NpcElement.MainHand, ref _mainHandActiveChildren);
        }
        
        void HideItemsInOffHand() {
            _restoreOffHandItems = true;
            AIUtils.HideItemsInHand(ParentModel.NpcElement.OffHand, ref _mainHandActiveChildren);
        }
        
        void RestoreMainHandItems() {
            AIUtils.RestoreItemsInHand(ref _mainHandActiveChildren);
        }
        
        void RestoreOffHandItems() {
            AIUtils.RestoreItemsInHand(ref _offHandActiveChildren);
        }
        
        // === Helpers
        void ReturnItemPrefab() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            _itemPrefab?.Return();
            _itemPrefab = null;
        }
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;

        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<UseItemBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour {
                get {
                    if (Behaviour.overrideAnimatorState) {
                        return Behaviour.animatorState.Yield();
                    }
                    return new[] { NpcStateType.UseItemMainHand, NpcStateType.UseItemOffHand };
                }
            }

            // === Constructor
            public Editor_Accessor(UseItemBehaviour behaviour) : base(behaviour) { }
        }
    }
}