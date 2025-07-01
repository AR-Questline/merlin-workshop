using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp {
    public class PExpNotification : PAdvancedNotification<ExpNotification, PExpNotificationData>, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _expText;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;

        public PExpNotification(VisualElement parent) : base(parent) { }

        protected override PExpNotificationData GetNotificationData() {
            return PresenterDataProvider.expNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(ExpNotification notification) {
            return CommonReferences.Get.AudioConfig.ExpAudio;
        }

        protected override void CacheVisualElements(VisualElement contentRoot) {
            _expText = contentRoot.Q<BetterOutlinedLabel>("exp-text");
        }

        protected override void OnBeforeShow(ExpNotification expNotification) {
            _expText.text = QuestUtils.GetGainedXPInfo(expNotification.gainedXP);
        }

        protected override Sequence ShowSequence() {
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration));
        }
    }
}