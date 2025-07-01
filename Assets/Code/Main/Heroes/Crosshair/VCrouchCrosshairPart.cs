using System;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.Crosshair {
    [UsesPrefab("UI/Crosshair/" + nameof(VCrouchCrosshairPart))]
    public class VCrouchCrosshairPart : VCrosshairPart<CrouchCrosshairPart>, UnityUpdateProvider.IWithUpdateGeneric {
        const float EnemiesAlertToVisibilityThreshold = 80f;
        const float FriendlyVisibilityWhenWatched = 0.8f;
        const float MinimumFriendlyVisibility = 0.1f;
        const float FriendlyVisibilityChangeSpeed = 3f;
        
        const float MinOrnamentScale = 0.3f;
        const float MinEyeAlpha = 0.1f;
        const float MaxEyeAlpha = 0.8f;
        const float TweenDuration = 0.2f;
        
        [SerializeField] Image eyeImage;
        [SerializeField] Image detectionOrnamentImage;
        [SerializeField] RectTransform detectionRoot;
        [SerializeField] GameObject notDetectionRoot;
        [SerializeField] TMP_Text detectionText;

        DelayedValue _friendlyVisibility;
        Tween _eyeColorTween;
        Tween _ornamentColorTween;
        float _prevVisibility = -1f;

        void Awake() {
            eyeImage.color = ARColor.Transparent;
            detectionOrnamentImage.color = ARColor.Transparent;
            detectionText.SetActiveAndText(false, LocTerms.SneakIndicatorDetected.Translate());
            detectionRoot.localScale = new Vector3(1, 0.1f, 1);
        }

        void OnEnable() {
            _friendlyVisibility.SetInstant(0);
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }
        
        void OnDisable() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
        }
        
        public void UnityUpdate() {
            if (TryRefreshVisibility(out float visibility)) {
                _prevVisibility = visibility;
                bool isDetected = visibility >= 1;

                var color = Color.Lerp(ARColor.DarkerGrey, ARColor.LightGrey, visibility);
                _ornamentColorTween.Kill();
                _ornamentColorTween = detectionOrnamentImage.DOColor(color, TweenDuration);
                
                color.a = Mathf.Approximately(visibility, 0) ? 0 : Mathf.Clamp(visibility, MinEyeAlpha, MaxEyeAlpha);
                _eyeColorTween.Kill();
                _eyeColorTween = eyeImage.DOColor(color, TweenDuration);
                
                detectionRoot.localScale = new Vector3(1, Mathf.Max(MinOrnamentScale, visibility), 1);
            
                detectionText.TrySetActiveOptimized(isDetected);
            
                bool notDetected = visibility <= 0;
                notDetectionRoot.SetActiveOptimized(notDetected);
                detectionRoot.TrySetActiveOptimized(!notDetected);
            }
        }

        bool TryRefreshVisibility(out float visibility) {
            visibility = 0;
            HeroCombat heroCombat = World.Any<HeroCombat>();
            if (heroCombat == null) return false;
            
            visibility = GetEasedVisibility(heroCombat);
            return !Mathf.Approximately(visibility, _prevVisibility);
        }
        
        float GetEasedVisibility(HeroCombat heroCombat) {
            const float PercentageAtEnter = 0.5f;
            const float AlertValueToEnter = StateAlert.Idle2LookAtPercentage / 100f;
            
            float visibility = GetVisibility(heroCombat);
            switch (visibility) {
                case <= 0f or >= 1f:
                    return visibility;
                case < AlertValueToEnter:
                    visibility *= 1 / AlertValueToEnter; // to make it go from 0 to 1.
                    visibility = Mathf.Lerp(0, PercentageAtEnter, visibility);
                    break;
                default:
                    visibility = (visibility - AlertValueToEnter) / (1 - AlertValueToEnter); // to make it go from 0 to 1.
                    visibility = Mathf.Lerp(PercentageAtEnter, 1, visibility);
                    break;
            }
            return visibility;
        }
        
        float GetVisibility(HeroCombat heroCombat) {
            if (heroCombat.IsHeroInFight) {
                return 1f;
            }

            var enemiesVisibility = heroCombat.MaxEnemiesAlert;
            if (enemiesVisibility > EnemiesAlertToVisibilityThreshold) {
                enemiesVisibility = heroCombat.MaxHeroVisibility > EnemiesAlertToVisibilityThreshold ? Mathf.Min(enemiesVisibility, heroCombat.MaxHeroVisibility) : EnemiesAlertToVisibilityThreshold;
            }

            var tracker = heroCombat.ParentModel.Element<IllegalActionTracker>();
            var currentFriendlyVisibility = tracker.IsBeingWatched
                ? Mathf.Lerp(MinimumFriendlyVisibility, FriendlyVisibilityWhenWatched, tracker.BeingWatchedLosePercent)
                : 0;
            _friendlyVisibility.Set(currentFriendlyVisibility);
            _friendlyVisibility.Update(Time.deltaTime, FriendlyVisibilityChangeSpeed); 
            
            return Math.Max(enemiesVisibility / 100f, _friendlyVisibility.Value);
        }

        protected override IBackgroundTask OnDiscard() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            UITweens.DiscardTween(ref _eyeColorTween);
            UITweens.DiscardTween(ref _ornamentColorTween);
            return base.OnDiscard();
        }
    }
}