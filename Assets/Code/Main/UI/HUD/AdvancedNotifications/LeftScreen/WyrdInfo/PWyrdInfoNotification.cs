using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility.UI;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo {
    public class PWyrdInfoNotification : PAdvancedNotification<WyrdInfoNotification, PWyrdInfoNotificationData>, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _informationText;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PWyrdInfoNotification(VisualElement parent) : base(parent) { }
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _informationText = contentRoot.Q<BetterOutlinedLabel>("wyrd-info-text");
        }

        protected override PWyrdInfoNotificationData GetNotificationData() {
            return PresenterDataProvider.wyrdInfoNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(WyrdInfoNotification notification) {
            return CommonReferences.Get.AudioConfig.WyrdInfoAudio;
        }

        protected override void OnBeforeShow(WyrdInfoNotification notification) {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            _informationText.text = notification.information.ToUpper();
            
            RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Long);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
        }

        protected override Sequence ShowSequence() {
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .Join(Content.DoMove(Vector3.zero, Data.MoveDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration));
        }
    }
}