using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using DG.Tweening;
using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.Utility.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroOnHitOverlay : ViewComponent<Hero> {
        [SerializeField] Image[] playerGotHitOverlays = Array.Empty<Image>();
        [SerializeField] CanvasGroup playerGotHitCanvasGroup;
        [SerializeField] float fadeOutDelayDuration = 2f;
        [SerializeField] float hideDuration = 0.5f;
        
        int MaxOverlays => playerGotHitOverlays.Length;
        Tween _hideTween;
        readonly List<Image> _currentOverlayImages = new();

        protected override void OnAttach() {
            Target.ListenTo(Stat.Events.StatChangedBy(AliveStatType.Health), OnHealthChangedBy, this);
            Target.ListenTo(Hero.Events.Revived, Hide, this);
            Target.ListenTo(VJailUI.Events.GoingToJail, Hide, this);
            
            playerGotHitCanvasGroup.alpha = 0f;
            _currentOverlayImages.Clear();
        }

        void OnHealthChangedBy(Stat.StatChange healthChange) {
            if (healthChange.value < 0) {
                ShowOverlay();
            }
        }

        void ShowOverlay() {
            Image nextOverlay = playerGotHitOverlays[RandomUtil.UniformInt(1, MaxOverlays) - 1];
            _hideTween.KillWithoutCallback();

            if (_currentOverlayImages.Contains(nextOverlay) == false) {
                _currentOverlayImages.Add(nextOverlay);
                nextOverlay.gameObject.SetActive(true);
            }
            
            playerGotHitCanvasGroup.alpha = 1f;
            _hideTween = playerGotHitCanvasGroup
                .DOFade(0, hideDuration)
                .SetDelay(fadeOutDelayDuration)
                .OnComplete(() => OnHideComplete(nextOverlay))
                .OnKill(Hide);
        }

        void OnHideComplete(Image imageToHide) {
            imageToHide.gameObject.SetActive(false);
            _currentOverlayImages.Remove(imageToHide);
        }

        void Hide() {
            playerGotHitCanvasGroup.alpha = 0f;
            foreach (Image image in _currentOverlayImages) {
                image.gameObject.SetActive(false);
            }
            _currentOverlayImages.Clear();
            _hideTween.KillWithoutCallback();
            _hideTween = null;
        }

        protected override void OnDiscard() {
            playerGotHitCanvasGroup.alpha = 0f;
            _currentOverlayImages.Clear();
            _hideTween.KillWithoutCallback();
            _hideTween = null;
        }
    }
}
