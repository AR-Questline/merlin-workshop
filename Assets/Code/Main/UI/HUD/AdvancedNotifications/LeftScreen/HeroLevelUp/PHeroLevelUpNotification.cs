using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp {
    public class PHeroLevelUpNotification : PAdvancedNotification<HeroLevelUpNotification, PLevelUpNotificationData>, IPresenterWithAccessibilityBackground {
        const float InitialBgOpacity = 0.85f;
        
        BetterOutlinedLabel _levelText;
        VisualElement _levelBgSmoke;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PHeroLevelUpNotification(VisualElement parent) : base(parent) { }
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _levelText = contentRoot.Q<BetterOutlinedLabel>("level-text");
            _levelBgSmoke = contentRoot.Q<VisualElement>("level-bg-smoke");
        }

        protected override PLevelUpNotificationData GetNotificationData() {
            return PresenterDataProvider.levelUpNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(HeroLevelUpNotification notification) {
            return CommonReferences.Get.AudioConfig.LevelUpAudio;
        }

        protected override void OnBeforeShow(HeroLevelUpNotification notification) {
            _levelBgSmoke.style.opacity = InitialBgOpacity;
            _levelText.text = $"{notification.heroLevel.ToString().ToUpper()} {LocTerms.LevelUp.Translate().ToUpper().PercentSizeText(80f)}" ;
            
            RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Long);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
        }

        protected override Sequence ShowSequence() {
            float bgFadeStartPosition = Data.FadeDuration + Data.VisibilityDuration / 2f;
            
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration))
                .Insert(bgFadeStartPosition, _levelBgSmoke.DoFade(0f, Data.BgFadeDuration));
        }
    }
}