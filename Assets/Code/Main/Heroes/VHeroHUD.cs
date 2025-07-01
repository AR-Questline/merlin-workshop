using System;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    [UsesPrefab("HUD/" + nameof(VHeroHUD))]
    public class VHeroHUD : View<Hero> {
        const float FadeSpeed = 2f;
        const float LastDamageTakenShowDuration = 4f;

        public GameObject arrowsCounter;
        public CanvasGroup content, heroBars, ammo, quickSlot;
        public Transform crosshairParent;
        public Transform heroSummonsParent;
        public Transform centerBars;

        [SerializeField] VCSelectedQuickSlot selectedQuickSlot;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD("HeroHUD");

        VCHeroHUDBar[] _heroBars;
        TextMeshProUGUI _arrows;
        WeakReference<Location> _locationPointedTowards;
        bool _initialized;
        HeroOverridesFSM _heroOverridesFsm;
        IEventListener _quiverListener;
        
        bool _isMapInteractive;
        bool? _showBars;
        bool _tookDamageLastly;
        float _lastDamageTakenTimer;
        HUDScale _hudScaleSetting;

        bool? ShowBars {
            get {
                if (_showBars != null) return _showBars;

                if (Target.HeroCombat.IsHeroInFight || _tookDamageLastly) {
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

        protected override void OnInitialize() {
            Target.AfterFullyInitialized(AfterFullyInitialized);
        }

        void AfterFullyInitialized() {
            _heroBars = GetComponentsInChildren<VCHeroHUDBar>();
            _arrows = arrowsCounter.GetComponentInChildren<TextMeshProUGUI>();

            var heroItems = Target.HeroItems;
            heroItems.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.MainHand), OnMainHandChanged,
                this);
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

            _lastDamageTakenTimer = LastDamageTakenShowDuration;
            Target.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, RestartLastDamageTakenTimer, this);
            
            _hudScaleSetting = World.Only<HUDScale>();
            UpdateHeroBarsScale();
            _hudScaleSetting.ListenTo(Setting.Events.SettingChanged, UpdateHeroBarsScale, this);
        }

        void UpdateHeroBarsScale() {
            heroBars.transform.localScale = Vector3.one * _hudScaleSetting.HeroBarsScale;
        }

        void RestartLastDamageTakenTimer() {
            _tookDamageLastly = true;
            _lastDamageTakenTimer = LastDamageTakenShowDuration;
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

            HandleLastDamageTakenTimer();

            float contentAlpha = _isMapInteractive ? 1 : 0;
            float heroBarsAlpha = ShowBars switch {
                true => 1,
                false => 0,
                _ => contentAlpha
            };
            
            float maxDelta = FadeSpeed * Time.unscaledDeltaTime;
            ChangeAlpha(content, contentAlpha, maxDelta);
            ChangeAlpha(heroBars, heroBarsAlpha, maxDelta);
            ChangeAlpha(ammo, heroBarsAlpha, maxDelta);
            ChangeAlpha(quickSlot, heroBarsAlpha, maxDelta);
        }
        
        void ChangeAlpha(CanvasGroup group, float targetAlpha, float maxDelta) {
            if (Mathf.Approximately(group.alpha, 0) && Mathf.Approximately(targetAlpha, 0)) {
                group.TrySetActiveOptimized(false);
                return;
            }

            if (Mathf.Approximately(group.alpha, 0) && Mathf.Approximately(targetAlpha, 1)) {
                group.TrySetActiveOptimized(true);
            }

            if (Mathf.Approximately(group.alpha, targetAlpha)) return;

            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, maxDelta);
        }

        void HandleLastDamageTakenTimer() {
            if (_tookDamageLastly) {
                _lastDamageTakenTimer = Mathf.Clamp(_lastDamageTakenTimer - Time.unscaledDeltaTime, 0f, LastDamageTakenShowDuration);
                if (_lastDamageTakenTimer <= 0f) {
                    _tookDamageLastly = false;
                    _lastDamageTakenTimer = LastDamageTakenShowDuration;
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
                _quiverListener = itemInQuiver.ListenTo(Item.Events.QuantityChanged, OnQuiverQuantityChanged, this);
            }

            OnQuiverQuantityChanged(new QuantityChangedData(itemInQuiver, itemInQuiver?.Quantity ?? 0));
        }

        void OnQuiverQuantityChanged(QuantityChangedData quantityChangedData) {
            _arrows.text = quantityChangedData.CurrentQuantity.ToString();
        }
    }
}