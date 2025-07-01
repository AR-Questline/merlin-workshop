using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterBow : CharacterWeapon {
        [SerializeField] Transform visualFirePoint;
        public Transform arrowController;

        public override Transform VisualFirePoint => visualFirePoint;
        protected override string[] LayersToEnable => layersToEnable;
        protected override ARAssetReference AnimatorControllerRef => Hero.TppActive ? animatorControllerRefTpp : animatorControllerRef;

        BowAnimatorSynchronize _bowAnimatorSynchronize;
        IPooledInstance _arrowInMainHand;
        IPooledInstance _arrowInCtrl;
        bool ArrowsSpawned => _arrowInMainHand != null || _arrowInCtrl != null;
        CancellationTokenSource _tokenSource;
        bool _isVerifying, _mainHandEnabled;
        
        public new static class Events {
            public static readonly Event<Hero, Hero> OnBowIdleEntered = new(nameof(OnBowIdleEntered));
        }
        
        protected override void AfterAnimatorLoaded() {
            base.AfterAnimatorLoaded();
            _bowAnimatorSynchronize = GetComponentInChildren<BowAnimatorSynchronize>();
            VerifyArrows().Forget();
        }

        protected override void AfterAnimationSpeedProcessed(int animatorParam, float modifier) {
            _bowAnimatorSynchronize.UpdateAnimatorParam(animatorParam, modifier);
        }
        
        protected override void AfterStoppedProcessingAnimationSpeed() { }
        
        public void OnEquipBow() {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnEquipBow();
            }
        }
        
        public void OnUnEquipBow() {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnUnEquipBow();
            }
        }

        public void ResetBowDrawSpeed() {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.SetBowDrawSpeed(1f);
            }
        }
        
        public void OnPullBow(float? bowDrawSpeed = null) {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnPullBow(bowDrawSpeed);
            }
        }

        public void OnHoldBow() {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnHoldBow();
            }
        }

        public void OnReleaseBow() {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnReleaseBow();
            }
        }

        public void OnBowIdle() {
            if (_bowAnimatorSynchronize != null && _bowAnimatorSynchronize.CanTransitionToIdle) {
                _bowAnimatorSynchronize.OnBowIdle();
            }
            VerifyArrows().Forget();

            if (Owner is Hero hero) {
                hero.Trigger(Events.OnBowIdleEntered, hero);
            }
        }

        public void OnBowDrawCancel(float normalizedTimeOffset) {
            if (_bowAnimatorSynchronize != null) {
                _bowAnimatorSynchronize.OnBowCancel(normalizedTimeOffset);
            }
        }
        
        protected override void OnAttachedToCustomHeroClothes(CustomHeroClothes clothes, ItemEquip equip) {
            base.OnAttachedToCustomHeroClothes(clothes, equip);
            
            Item quiver = equip.Item.CharacterInventory.EquippedItem(EquipmentSlotType.Quiver);
            if (quiver is not { Quantity: > 0 }) {
                return;
            }
            
            ItemProjectile arrowProjectile = quiver.TryGetElement<ItemProjectile>();
            ShareableARAssetReference arrowRef = arrowProjectile?.Data.visualPrefab ?? ItemProjectile.DefaultVisualRef;
            clothes.SpawnWeapon(arrowRef.Get(), quiver.Element<ItemEquip>(), clothes.MainHandSocket);
        }
        
        protected override void OnDetachedFromCustomHeroClothes(CustomHeroClothes clothes) {
            clothes.DespawnWeapon(EquipmentSlotType.Quiver);
        }
        
        async UniTaskVoid VerifyArrows() {
            if (_isVerifying) {
                return;
            }

            _isVerifying = true;
            if (Owner is Hero h) {
                Item quiver = h.HeroItems.EquippedItem(EquipmentSlotType.Quiver);
                bool hasArrows = quiver?.Quantity > 0;
                if (hasArrows && !ArrowsSpawned) {
                    await ClearInstancedArrows(false);
                    
                    ItemProjectile itemProjectile = quiver.TryGetElement<ItemProjectile>();
                    var mainHandArrowHandle = itemProjectile?.GetInHandProjectile(null, null) ?? ItemProjectile.GetDefaultInHandProjectile(null, null);
                    var ctrlArrowHandle = itemProjectile?.GetInHandProjectile(null, null) ?? ItemProjectile.GetDefaultInHandProjectile(null, null);
                    (IPooledInstance mainHandArrow, IPooledInstance ctrlArrow) = await UniTask.WhenAll(mainHandArrowHandle, ctrlArrowHandle);
                    
                    _arrowInMainHand = mainHandArrow;
                    _arrowInCtrl = ctrlArrow;
                    
                    if (this == null || HasBeenDiscarded) {
                        _arrowInMainHand?.Return();
                        _arrowInCtrl?.Return();
                        return;
                    }
                    
                    _arrowInMainHand.Instance.transform.SetParent(h.MainHand, false);
                    _arrowInCtrl.Instance.transform.SetParent(arrowController, false);
                    ConfigureArrowInstance(_arrowInMainHand);
                    ConfigureArrowInstance(_arrowInCtrl);
                    ToggleArrows(_mainHandEnabled);
                } else if (!hasArrows && ArrowsSpawned) {
                    ClearInstancedArrows(false).Forget();
                }
            }
            _isVerifying = false;
        }

        // === Helpers
        public void ToggleArrows(bool mainHandEnabled) {
            _mainHandEnabled = mainHandEnabled;
            if (_arrowInMainHand?.Instance != null) {
                _arrowInMainHand.Instance.SetActive(mainHandEnabled);
            }

            if (_arrowInCtrl?.Instance != null) {
                _arrowInCtrl.Instance.SetActive(!mainHandEnabled);
            }
        }

        async UniTask ClearInstancedArrows(bool waitForCompletion = true) {
            if (waitForCompletion && _isVerifying) {
                await AsyncUtil.WaitWhile(gameObject, () => _isVerifying);
            }
            _arrowInMainHand?.Return();
            _arrowInMainHand = null;
            _arrowInCtrl?.Return();
            _arrowInCtrl = null;
        }

        static void ConfigureArrowInstance(IPooledInstance arrow) {
            foreach (Transform t in arrow.Instance.GetComponentsInChildren<Transform>(true)) {
                t.gameObject.layer = RenderLayers.IgnoreRaycast;
            }
        }
        
        // === Discarding

        protected override IBackgroundTask OnDiscard() {
            ClearInstancedArrows().Forget();
            return base.OnDiscard();
        }
    }
}