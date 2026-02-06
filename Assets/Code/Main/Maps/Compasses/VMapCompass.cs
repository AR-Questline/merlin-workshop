using Awaken.TG.Assets;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using DG.Tweening;
using Sirenix.OdinInspector;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Compasses {
    [UsesPrefab("HUD/Map/VMapCompass")]
    public class VMapCompass : View<Compass> {
        [SerializeField] VCompassElement compassElementPrefab;
        [SerializeField] Transform markerParent;
        [SerializeField] float rangeMultiplier;
        [SerializeField] CanvasGroup content;
        [SerializeField] CanvasGroup searchingImageCanvasGroup;
        // [SerializeField] Gradient2 searchingGradient;
        
        CanvasGroup _compass;
        Tween _contentTween;
        HUDScale _hudScaleSetting;
        Sequence _searchingSequence;

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
            Target.ListenTo(Compass.Events.SearchAreaStateChanged, OnSearchAreaStateChanged, this);
            OnSearchAreaStateChanged(false);
            
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

        void OnSearchAreaStateChanged(bool state) {
            // searchingGradient.Zoom = state ? 0f : 1f;
            // _searchingSequence.Kill();
            // _searchingSequence = DOTween.Sequence().SetUpdate(true)
            //     .Append(searchingImageCanvasGroup.DOFade(state ? 1f : 0f, 0.3f))
            //     .Join(DOTween.To(() => searchingGradient.Zoom, x => searchingGradient.Zoom = x, state ? 1f : 0f, 0.3f));
        }
    }
}