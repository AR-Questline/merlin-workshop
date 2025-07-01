using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public class VCMapTooltip : ViewComponent<MapUI> {
        [SerializeField] TMP_Text _tooltipText;
        [SerializeField] CanvasGroup _fadeGroup;

        Tween _fadeTween;

        protected override void OnAttach() {
            _fadeGroup.alpha = 0;
            Target.ListenTo(MapSceneUI.Events.SelectedMarkerChanged, UpdateTooltip, this);
        }

        void UpdateTooltip(MapMarker marker) {
            if (marker != null) {
                SetText(marker.DisplayName);
            } else {
                Hide();
            }
        }

        void SetText(string text) {
            if (string.IsNullOrEmpty(text)) {
                Hide();
                return;
            }
            _tooltipText.text = text;
            Show();
        }

        void Hide() {
            Fade(0, Ease.InQuad);
        }

        void Show() {
            Fade(1, Ease.OutQuad);
        }

        void Fade(float alpha, Ease ease) {
            _fadeTween.Kill();
            _fadeTween = _fadeGroup.DOFade(alpha, VMapUI.TooltipFadeDuration)
                .SetUpdate(true)
                .SetEase(ease);
        }
    }
}
