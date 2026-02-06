using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes {
    [UsesPrefab("HUD/" + nameof(VHeroHUD))]
    public class VHeroHUD : View<Hero> {
        const float FadeSpeed = 2f;
        const float ShowHUDDuration = 4f;

        [SerializeField] GameObject arrowsCounter;
        [SerializeField] Image arrowsImage;
        [SerializeField] CanvasGroup content, heroBars;
        [SerializeField] CanvasGroup[] hidableGroups = Array.Empty<CanvasGroup>();
        [SerializeField] Transform crosshairParent;
        [SerializeField] Transform heroSummonsParent;
        [SerializeField] Transform centerBars;
        [SerializeField] VCSelectedQuickSlot selectedQuickSlot;
        [SerializeField] float baseBarsScale = 0.8f;
        
        VCHeroHUDBar[] _heroBars;
        TextMeshProUGUI _arrows;

        SpriteReference _arrowsSpriteReference;
        WeakReference<Location> _locationPointedTowards;
        bool _initialized;
        HeroOverridesFSM _heroOverridesFsm;
        IEventListener _quiverListener;
        bool _isMapInteractive;
        bool? _showBars;
        bool _hudRefreshedLastly;
        float _showHUDTimer;
        HUDScale _hudScaleSetting;
        
        public Transform CenterBars => centerBars;
        public Transform HeroSummonsParent => heroSummonsParent;
        public Transform CrosshairParent => crosshairParent;
        
        Vector3 BaseBarsScale => Vector3.one * baseBarsScale;
        bool? ShowBars {
            get {
                if (_showBars != null) return _showBars;

                if (Target.HeroCombat.IsHeroInFight || _hudRefreshedLastly) {
                    return true;
                }

                foreach (var heroBar in _heroBars) {
                    if (heroBar.ForceShow) {
                        return true;
                    }
                }

                return Target.MainHandWeapon != null &&
                       (
                           Target.MainHandWeapon.isActiveAndEnabled ||
                           Target.MainHandWeapon.IsLoadingAnimator ||
                           Target.TryGetCachedElement(ref _heroOverridesFsm)?.CurrentStateType == HeroStateType.ThrowableThrow
                       );
            }
        }
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD("HeroHUD");

        protected override void OnInitialize() {
            Target.AfterFullyInitialized(AfterFullyInitialized);
        }

        void AfterFullyInitialized() {
            _heroBars = GetComponentsInChildren<VCHeroHUDBar>();
            _arrows = arrowsCounter.GetComponentInChildren<TextMeshProUGUI>();

            var heroItems = Target.HeroItems;
            heroItems.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.MainHand), OnMainHandChanged, this);
            heroItems.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.Quiver), OnQuiverChanged, this);

            var uiStack = UIStateStack.Instance;
            uiStack.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            
            _isMapInteractive = uiStack.State.IsMapInteractive;
            _showBars = uiStack.State.ForceShowHeroBars;

            OnMainHandChanged(heroItems);
            OnQuiverChanged(heroItems);

            _isMapInteractive = uiStack.State.IsMapInteractive;
            _showBars = uiStack.State.ForceShowHeroBars;
            _initialized = true;

            InitShowHUDTimer();
            
            _hudScaleSetting = World.Only<HUDScale>();
            UpdateHeroBarsScale();
            _hudScaleSetting.ListenTo(Setting.Events.SettingChanged, UpdateHeroBarsScale, this);
        }

        void InitShowHUDTimer() {
            _showHUDTimer = ShowHUDDuration;
            Target.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, RestartShowHUDTimer, this);
            Target.ListenTo(HeroItems.Events.QuickSlotUsed, RestartShowHUDTimer, this);
            Target.ListenTo(HeroItems.Events.QuickSlotSelected, RestartShowHUDTimer, this);
            Target.Development.SarrasHeroTreeBranches.ListenTo(SarrasHeroTreeBranches.Events.TalentTreeBranchChanged, RestartShowHUDTimer, this);
        }

        void UpdateHeroBarsScale() {
            heroBars.transform.localScale = BaseBarsScale * _hudScaleSetting.HeroBarsScale;
        }

        void RestartShowHUDTimer() {
            _hudRefreshedLastly = true;
            _showHUDTimer = ShowHUDDuration;
        }

        void OnUIStateChanged(UIState state) {
            _isMapInteractive = state.IsMapInteractive;
            if (_isMapInteractive) {
                _showBars = state.ForceShowHeroBars;
            } else {
                _showBars = state.ForceShowHeroBars ?? false;
            }

            if ((ShowBars ?? false) && Target?.HeroItems is { IsInitialized: true }) {
                selectedQuickSlot.UpdateIcon();
            }
        }

        void Update() {
            if (!_initialized) {
                return;
            }

            HandleShowHUDTimer();

            float contentAlpha = _isMapInteractive ? 1 : 0;
            float heroBarsAlpha = ShowBars switch {
                true => 1,
                false => 0,
                _ => contentAlpha
            };
            
            float maxDelta = FadeSpeed * Time.unscaledDeltaTime;
            ChangeAlpha(content, contentAlpha, maxDelta, ShowBars ?? false);
            foreach (CanvasGroup hidableGroup in hidableGroups) {
                ChangeAlpha(hidableGroup, heroBarsAlpha, maxDelta);
            }
        }
        
        void ChangeAlpha(CanvasGroup group, float targetAlpha, float maxDelta, bool forceShow = false) {
            if (!forceShow && Mathf.Approximately(group.alpha, 0) && Mathf.Approximately(targetAlpha, 0)) {
                group.TrySetActiveOptimized(false);
                return;
            }

            if (Mathf.Approximately(group.alpha, 0) && Mathf.Approximately(targetAlpha, 1)) {
                group.TrySetActiveOptimized(true);
            }

            if (Mathf.Approximately(group.alpha, targetAlpha)) return;

            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, maxDelta);
        }

        void HandleShowHUDTimer() {
            if (_hudRefreshedLastly) {
                _showHUDTimer = Mathf.Clamp(_showHUDTimer - Time.unscaledDeltaTime, 0f, ShowHUDDuration);
                if (_showHUDTimer <= 0f) {
                    _hudRefreshedLastly = false;
                    _showHUDTimer = ShowHUDDuration;
                }
            }
        }

        void OnMainHandChanged(ICharacterInventory inventory) {
            var mainHandItem = inventory.EquippedItem(EquipmentSlotType.MainHand);
            arrowsCounter.SetActive(mainHandItem is { IsRanged: true });
        }

        void OnQuiverChanged(ICharacterInventory inventory) {
            var itemInQuiver = inventory.EquippedItem(EquipmentSlotType.Quiver);
            World.EventSystem.TryDisposeListener(ref _quiverListener);
            
            if (itemInQuiver != null) {
                TryToSetupArrowsSprite(itemInQuiver);
                _quiverListener = itemInQuiver.ListenTo(Item.Events.QuantityChanged, OnQuiverQuantityChanged, this);
            }

            OnQuiverQuantityChanged(new QuantityChangedData(itemInQuiver, itemInQuiver?.Quantity ?? 0));
        }

        void TryToSetupArrowsSprite(Item arrow) {
            _arrowsSpriteReference?.Release();
            if (arrow.Icon is { IsSet: true } icon) {
                _arrowsSpriteReference = icon.Get();
                _arrowsSpriteReference.SetSprite(arrowsImage);
            }
        }

        void OnQuiverQuantityChanged(QuantityChangedData quantityChangedData) {
            _arrows.text = quantityChangedData.CurrentQuantity.ToString();
        }

        protected override IBackgroundTask OnDiscard() {
            _arrowsSpriteReference?.Release();
            return base.OnDiscard();
        }
    }
}