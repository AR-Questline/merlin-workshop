using Awaken.TG.Assets;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Maps.Compasses {
    [UsesPrefab("HUD/Map/VMapCompass")]
    public class VMapCompass : View<Compass> {
        [SerializeField] VCompassElement compassElementPrefab;
        [SerializeField] Transform markerParent;
        [SerializeField] float rangeMultiplier;
        [SerializeField] CanvasGroup content;
        [SerializeField] Image[] defaultImages = Array.Empty<Image>();
        [SerializeField] Image trespassingImage;
        [SerializeField] Image searchingImage;
        
        CanvasGroup _compass;
        Tween _contentTween;
        Tween _trespassingTween;
        HUDScale _hudScaleSetting;

        [BoxGroup("World Directions")][ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)] public ShareableSpriteReference north;
        [BoxGroup("World Directions")][ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)] public ShareableSpriteReference east;
        [BoxGroup("World Directions")][ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)] public ShareableSpriteReference south;
        [BoxGroup("World Directions")][ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)] public ShareableSpriteReference west;

        public VCompassElement CompassElementPrefab => compassElementPrefab;
        public Transform MarkerParent => markerParent;
        public float RangeMultiplier => rangeMultiplier;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMapCompass();

        protected override void OnInitialize() {
            _compass = GetComponent<CanvasGroup>();
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            Target.ListenTo(Compass.Events.VisualStateChanged, OnVisualStateChanged, this);
            Target.ListenTo(Compass.Events.SearchAreaStateChanged, OnSearchAreaStateChanged, this);
            OnSearchAreaStateChanged(false);
            trespassingImage.CrossFadeAlpha(0, 0f, true);
            
            if (!World.Only<ShowUIHUD>().CompassEnabled) {
                content.alpha = 0;
            }
        }

        protected override void OnFullyInitialized() {
            _hudScaleSetting = World.Only<HUDScale>();
            UpdateHeroBarsScale();
            _hudScaleSetting.ListenTo(Setting.Events.SettingChanged, UpdateHeroBarsScale, this);
        }
        
        void UpdateHeroBarsScale() {
            transform.localScale = Vector3.one * _hudScaleSetting.CompassScale;
        }

        void OnUIStateChanged(UIState state) {
            bool compassEnabledInSettings = World.Only<ShowUIHUD>().CompassEnabled;
            bool hudStateAllowsCompass = !state.HudState.HasFlag(HUDState.CompassHidden);
            _compass.alpha = (compassEnabledInSettings && hudStateAllowsCompass) ? 1 : 0;
            
            if (content.alpha == 1 && state.IsMapInteractive || content.alpha == 0 && !state.IsMapInteractive) {
                _contentTween.Kill();
                return;
            }
            
            _contentTween.Kill();
            _contentTween = DOTween.To(() => content.alpha, a => content.alpha = a, state.IsMapInteractive ? 1 : 0, 0.5f);
        }

        void OnVisualStateChanged(CompassVisualState visualState) {
            if (visualState.trespassing) {
                foreach (var img in defaultImages) {
                    img.CrossFadeAlpha(0, 0.25f, true);
                }
                trespassingImage.CrossFadeAlpha(1, 0.25f, true);
                trespassingImage.color = trespassingImage.color.WithAlpha(0.4f);
                _trespassingTween = trespassingImage.DOFade(1, 0.7f).SetLoops(-1, LoopType.Yoyo);
            } else {
                foreach (var img in defaultImages) {
                    img.CrossFadeAlpha(1, 0.25f, true);
                }
                _trespassingTween.Kill();
                trespassingImage.CrossFadeAlpha(0, 0.25f, true);
            }
        }

        void OnSearchAreaStateChanged(bool state) {
            searchingImage.CrossFadeAlpha(state ? 1 : 0, 0.25f, true);
        }
    }
}