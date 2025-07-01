using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours {
    [Serializable]
    public partial class ThrowItemBehaviour : AttackBehaviour<EnemyBaseClass> {
        const NpcStateType MainHandAnimatorState = NpcStateType.ThrowItemMainHand;
        const NpcStateType OffHandAnimatorState = NpcStateType.ThrowItemOffHand;
        const string ThrowParametersGroup = "Throw Parameters";
        const string AssetsSetupGroup = "Assets Setup";
        const string AnimationSettingsGroup = "Animation Settings";
        const int IgnoreConditionsWhenHeightDifferenceAbove = 2;

        [SerializeField, FoldoutGroup(ThrowParametersGroup)] bool canBePrevented = true;
        [SerializeField, FoldoutGroup(ThrowParametersGroup), Range(0f, 1f)] float triggeringChanceAfterMelee = 0.5f;
        [SerializeField, FoldoutGroup(ThrowParametersGroup)] int maxItemThrowsInRow = 1;
        [SerializeField, FoldoutGroup(ThrowParametersGroup)] bool overrideFireAngleRange;
        [SerializeField, FoldoutGroup(ThrowParametersGroup), ShowIf(nameof(overrideFireAngleRange))] FloatRange fireAngleRange = new(-60f, 60f);
        [SerializeField, FoldoutGroup(ThrowParametersGroup)] float maxItemVelocity = 25f;
        [SerializeField, FoldoutGroup(ThrowParametersGroup)] float projectileVelocityMultiplier = 1.5f;
        [SerializeField, FoldoutGroup(ThrowParametersGroup)] bool predictPlayerMovement;
        [SerializeField, FoldoutGroup(ThrowParametersGroup), Range(0, 1f), InfoBox("0 - aim at enemy feet, 1 - aim at enemy head")] 
        float aimAtEnemyHeight = 0.8f;
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultRangedAttackData;
        [SerializeField, FoldoutGroup(AssetsSetupGroup)] bool checkItemInInventory;
        [SerializeField, FoldoutGroup(AssetsSetupGroup)] bool spendAmmoFromInventory;
        [SerializeField, FoldoutGroup(AssetsSetupGroup)] ItemProjectileAttachment.ItemProjectileData customProjectile;
        [SerializeField, FoldoutGroup(AnimationSettingsGroup)] bool overrideHand;
        [SerializeField, FoldoutGroup(AnimationSettingsGroup), ShowIf(nameof(overrideHand))] CastingHand overridenCastingHand = CastingHand.OffHand;
        [SerializeField, FoldoutGroup(AnimationSettingsGroup), ShowIf(nameof(overrideHand))] bool overrideAnimatorState;
        [SerializeField, FoldoutGroup(AnimationSettingsGroup), ShowIf(nameof(OverrideState))] NpcStateType animatorState = NpcStateType.ThrowItemOffHand;

        public float TriggeringChanceAfterMelee => triggeringChanceAfterMelee;

        public override bool CanBeUsed => ItemPossession && CanSeeTarget && (ParentModel.HasUnreachablePathToHeroFromCombatSlotCondition() || CanBeUsedReachable);
        protected override bool IgnoreBaseConditions {
            get {
                var target = ParentModel.NpcElement?.GetCurrentTarget();
                return target != null && math.abs(target.Coords.y - ParentModel.Coords.y) >= IgnoreConditionsWhenHeightDifferenceAbove;
            }
        }
        bool CanBeUsedReachable => !_preventUsage && ThrowsInRowCondition;
        bool CanSeeTarget {
            get {
                var target = ParentModel.NpcElement?.GetCurrentTarget();
                return target != null && ParentModel.NpcAI.CanSee(target.AIEntity, false) != VisibleState.Covered;
            }
        }
        protected override NpcStateType StateType => _animatorStateType;
        protected override MovementState OverrideMovementState => new NoMoveAndRotateTowardsTarget();
        bool ThrowsInRowCondition => _itemThrowsInRow < maxItemThrowsInRow;
        bool OverrideState => overrideHand && overrideAnimatorState;
        bool ItemPossession => checkItemInInventory ? HasInventoryItemSet : HasCustomItemSet;
        bool HasCustomItemSet => customProjectile.logicPrefab is {IsSet: true};
        bool HasInventoryItemSet {
            get {
                if (_itemsNotAddedToInventoryYet) {
                    VerifyInventoryItems();
                }
                return _throwableItem is { HasBeenDiscarded: false } &&
                       _projectile is { HasBeenDiscarded: false };
            }
        }

        int _itemThrowsInRow;
        bool _preventUsage;
        bool _itemsNotAddedToInventoryYet;
        CancellationTokenSource _cancellationToken;
        IPooledInstance _itemPrefab;
        ProjectilePreload _preloadedProjectile;
        bool _restoreMainHandItems;
        bool _restoreOffHandItems;
        HashSet<GameObject> _mainHandActiveChildren;
        HashSet<GameObject> _offHandActiveChildren;
        Item _throwableItem;
        ItemProjectile _projectile;
        CastingHand _castingHand;
        NpcStateType _animatorStateType;

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.ListenTo(EnemyBaseClass.Events.BehaviourStarted, OnBehaviourStarted, this);
            _mainHandActiveChildren = new HashSet<GameObject>();
            _offHandActiveChildren = new HashSet<GameObject>();
            
            //don't run this behaviour as a first one during combat
            PreventUsageTillNextAttackBehaviour();
            
            if (checkItemInInventory) {
                VerifyInventoryItems();
            }
        }

        void VerifyInventoryItems() {
            if (!Npc.ItemsAddedToInventory) {
                _itemsNotAddedToInventoryYet = true;
                return;
            }
            _itemsNotAddedToInventoryYet = false;
            _throwableItem = ParentModel.NpcElement.Inventory.Items.FirstOrDefault(i => i.IsThrowable);
            _projectile = _throwableItem?.TryGetElement<ItemProjectile>();
        }
        
        void OnBehaviourStarted(IBehaviourBase combatBehaviour) {
            if (combatBehaviour != this && !combatBehaviour.IsPeaceful && combatBehaviour is AttackBehaviour) {
                _preventUsage = false;
                _itemThrowsInRow = 0;
            }
        }
        
        public void PreventUsageTillNextAttackBehaviour() {
            if (!canBePrevented) {
                return;
            }
            
            _preventUsage = true;
        }

        // === LifeCycle
        protected override bool OnStart() {
            _itemThrowsInRow++;
            _animatorStateType = DetermineAnimationState();
            return true;
        }

        public override void OnStop() {
            base.OnStop();
            ReturnItemPrefabInstantly();
            
            if (_restoreMainHandItems) {
                RestoreMainHandItems();
            }

            if (_restoreOffHandItems) {
                RestoreOffHandItems();
            }
        }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                SpawnItemInHand().Forget();
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                SpawnProjectile();
                if (spendAmmoFromInventory) {
                    _throwableItem?.DecrementQuantity();
                }
            }
        }
        
        public override void BehaviourInterrupted() {
            ReturnItemPrefabInstantly();
        }

        protected override void OnAnimatorExitDesiredState() {
            ParentModel.StopCurrentBehaviour(true);
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ReturnItemPrefabInstantly();
            
            // decrease throwable quantity for chance to drop 0 items (loot table draw quantity between [1-n])
            if (!spendAmmoFromInventory && _throwableItem?.Quantity > 0) {
                _throwableItem.DecrementQuantity();
            }
        }
        
        // === Helpers
        async UniTaskVoid SpawnItemInHand() {
            Transform hand;
            if (overrideHand) {
                if (overridenCastingHand == CastingHand.MainHand) {
                    UseMainHand(true);
                } else {
                    UseOffHand(true);
                }
                _castingHand = overridenCastingHand;
            } else if (ParentModel.NpcElement.MainHand.childCount <= 0) {
                UseMainHand(false);
                _castingHand = CastingHand.MainHand;
            } else if (ParentModel.NpcElement.OffHand.childCount <= 0) {
                UseOffHand(false);
                _castingHand = CastingHand.OffHand;
            } else {
                UseMainHand(true);
                _castingHand = CastingHand.MainHand;
            }
            
            void UseMainHand(bool hideItems) {
                hand = ParentModel.NpcElement.MainHand;
                if(hideItems) { HideItemsInMainHand(); }
            }

            void UseOffHand(bool hideItems) {
                hand = ParentModel.NpcElement.OffHand;
                if(hideItems) { HideItemsInOffHand(); }
            }
            
            _cancellationToken = new CancellationTokenSource();
            _preloadedProjectile.Release();
            if (checkItemInInventory && _projectile != null) {
                _itemPrefab = await _projectile.GetInHandProjectile(hand, _cancellationToken);
                _preloadedProjectile = _projectile.PreloadProjectile();
            } else {
                _itemPrefab = await ItemProjectile.GetCustomInHandProjectile(customProjectile.visualPrefab, hand, _cancellationToken);
                _preloadedProjectile = ItemProjectile.PreloadCustomProjectile(customProjectile.logicPrefab.Get(), customProjectile.visualPrefab);
            }
        }

        NpcStateType DetermineAnimationState() {
            if (overrideHand) {
                if (overrideAnimatorState) {
                    _animatorStateType = animatorState;
                }
            } else if (ParentModel.NpcElement.MainHand.childCount <= 0) {
                _animatorStateType = MainHandAnimatorState;
            } else if (ParentModel.NpcElement.OffHand.childCount <= 0) {
                _animatorStateType = OffHandAnimatorState;
            } else {
                _animatorStateType = MainHandAnimatorState;
            }

            return _animatorStateType;
        }
        
        void SpawnProjectile() {
            Vector3 spawnPosition;
            var npcElement = ParentModel.NpcElement;

            if (_itemPrefab?.Instance != null) {
                spawnPosition = _itemPrefab.Instance.transform.position;
            } else {
                Transform hand = _castingHand == CastingHand.MainHand ? npcElement.MainHand : npcElement.OffHand;
                spawnPosition = hand.position;
            }

            var parentView = npcElement.CharacterView;
            var target = npcElement.GetCurrentTarget();
            float additionalVelocityMultiplier = target != null ? (target.Coords.y - ParentModel.Coords.y).Remap(0, 10, 0, 3, true) : 0;
            var fireParams = new CombatBehaviourUtils.FireProjectileParams {
                shooterView = parentView,
                target = target,
                fireAngleRange = overrideFireAngleRange ? fireAngleRange : CombatBehaviourUtils.DefaultFireAngleRange,
                aimAtEnemyHeight = aimAtEnemyHeight,
                maxVelocity = maxItemVelocity,
                velocityMultiplier = projectileVelocityMultiplier + additionalVelocityMultiplier,
                predictPlayerMovement = predictPlayerMovement,
                parabolicShot = true
            };
            var shootParams = VGUtils.ShootParams.Default;
            shootParams.shooter = npcElement;
            if (_projectile is { HasBeenDiscarded: false }) {
                shootParams = shootParams.WithItem(_projectile.ParentModel);
            } else {
                shootParams = shootParams.WithCustomProjectile(customProjectile.ToProjectileData());
            }
            shootParams.startPosition = spawnPosition;
            shootParams.projectileSlotType = EquipmentSlotType.Throwable;
            shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
            shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
            var projectileWrapper = CombatBehaviourUtils.FireProjectile(fireParams, shootParams);

            ReturnItemPrefabWhenProjectileReady(projectileWrapper).Forget();
        }
        
        void HideItemsInMainHand() {
            _restoreMainHandItems = true;
            AIUtils.HideItemsInHand(ParentModel.NpcElement.MainHand, ref _mainHandActiveChildren);
        }
        
        void HideItemsInOffHand() {
            _restoreOffHandItems = true;
            AIUtils.HideItemsInHand(ParentModel.NpcElement.OffHand, ref _offHandActiveChildren);
        }
        
        void RestoreMainHandItems() {
            AIUtils.RestoreItemsInHand(ref _mainHandActiveChildren);
        }
        
        void RestoreOffHandItems() {
            AIUtils.RestoreItemsInHand(ref _offHandActiveChildren);
        }
        
        async UniTaskVoid ReturnItemPrefabWhenProjectileReady(ProjectileWrapper projectileWrapper) {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            _preloadedProjectile.Release();

            if (_itemPrefab != null) {
                var itemPrefabToReturn = _itemPrefab;
                _itemPrefab = null;
                
                await projectileWrapper.WaitForProjectileInstanceToLoad();
                itemPrefabToReturn.Return();
            }
        }

        void ReturnItemPrefabInstantly() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            _itemPrefab?.Return();
            _itemPrefab = null;
            _preloadedProjectile.Release();
        }
    }
}