using Awaken.Utility;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    public partial class ItemThrowable : Element<Item>, IRefreshedByAttachment<ItemThrowableAttachment>, IItemAction {
        public override ushort TypeForSerialization => SavedModels.ItemThrowable;

        ItemThrowableAttachment _spec;
        ItemProjectile _projectile;
        
        IPooledInstance _itemPrefab;
        CancellationTokenSource _itemSpawnToken;
        IEventListener _throwEventListener;
        ProjectilePreload _preloadedProjectile;
        
        public ItemActionType Type => ItemActionType.Use;
        
        public new static class Events {
            public static readonly Event<IItemOwner, bool> ThrowableThrown = new(nameof(ThrowableThrown));
            public static readonly Event<IItemOwner, bool> ThrowableThrowAnimationEnded = new(nameof(ThrowableThrowAnimationEnded));
        }
        
        public void InitFromAttachment(ItemThrowableAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            _projectile = ParentModel.TryGetElement<ItemProjectile>();
        }

        // === IItemAction
        public void Submit() {
            if (ParentModel.Owner is Hero { CanUseEquippedWeapons: true } h && _throwEventListener == null) {
                var fsm = h.Element<HeroOverridesFSM>();
                fsm.SetCurrentState(HeroStateType.ThrowableThrow, 0);
                
                _throwEventListener = h.ListenToLimited(Events.ThrowableThrown, Throw, this);
                h.ListenToLimited(Events.ThrowableThrowAnimationEnded, ThrowAnimationEnded, this);

                World.Any<CharacterSheetUI>()?.Discard();
                World.Any<QuickUseWheelUI>()?.Discard();
                InstantiateVisual().Forget();
            }
        }
        public void AfterPerformed() {}
        public void Perform() { }
        public void Cancel() { }
        
        // --- ThrowLogic

        async UniTaskVoid InstantiateVisual() {
            if (ParentModel.Owner is Hero hero) {
                _itemSpawnToken?.Cancel();
                _itemSpawnToken = new CancellationTokenSource();
                _itemPrefab = await _projectile.GetInHandProjectile(hero.MainHand, _itemSpawnToken);
                if (_itemPrefab.Instance != null) {
                    _preloadedProjectile = _projectile.PreloadProjectile();
                    var item = _itemPrefab.Instance;
                    item.transform.SetParent(item.transform.parent.parent);
                    item.SetActive(true);
                }
            }
        }

        void Throw() {
            if (_throwEventListener != null) {
                World.EventSystem.DisposeListener(ref _throwEventListener);
            }
            
            if (_itemPrefab == null) {
                return;
            }

            var itemTransform = _itemPrefab.Instance?.transform;
            var itemPosition = itemTransform?.position;
            var itemRotation = itemTransform?.rotation;
            _itemPrefab.Instance?.SetActive(false);
            _itemPrefab?.Return();
            _itemPrefab = null;
            
            if (ParentModel.Owner is Hero hero) {
                var vHeroController = hero.VHeroController;

                var heroFirePoint = vHeroController.FirePoint;
                Vector3 shotPosition = itemPosition ?? heroFirePoint.position;
                
                var velocity = BowFSM.CalculateArrowVelocity(shotPosition, _spec.ThrowForce, out var offsetData);

                Instantiate(shotPosition, velocity, offsetData, heroFirePoint, itemRotation).Forget();
            }
        }

        async UniTaskVoid Instantiate(Vector3 shotPosition, Vector3 velocity, ProjectileOffsetData? offsetData, Transform firePoint, Quaternion? itemRotation) {
            CombinedProjectile result = await _projectile.GetProjectile(shotPosition, Quaternion.LookRotation(shotPosition, shotPosition + velocity), true, null, null, null);
            
            if (result.visual.Instance != null && result.logic != null) {
                DamageDealingProjectile projectile = result.logic.GetComponent<DamageDealingProjectile>();
                projectile.SetVelocityAndForward(velocity, offsetData);
                        
                if (itemRotation.HasValue && projectile is ThrowingKnife knife) {
                    knife.SetRotation(itemRotation.Value);
                    knife.SetItemTemplate(ParentModel.Template);
                } else if (projectile is Arrow arrow) {
                    arrow.SetItemTemplate(ParentModel.Template);
                }

                var hero = Hero.Current;
                projectile.owner = hero;
                projectile.SetBaseDamageParams(ParentModel, null, 1f, ParentModel.ItemStats.DamageTypeData);
                projectile.FinalizeConfiguration();
                        
                ParentModel.DecrementQuantityWithoutNotification();
                        
                hero.Trigger(HeroItems.Events.QuickSlotItemUsedWithDelay, hero);
            }
        }

        void ThrowAnimationEnded() {
            Cleanup();
        }

        void Cleanup() {
            _itemSpawnToken?.Cancel();
            _itemSpawnToken = null;
            
            _itemPrefab?.Return();
            _itemPrefab = null;

            _preloadedProjectile.Release();

            if (_throwEventListener != null) {
                World.EventSystem.DisposeListener(ref _throwEventListener);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Cleanup();
        }
    }
}