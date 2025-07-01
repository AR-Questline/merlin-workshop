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
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery {
    public class PLocationNotification : PAdvancedNotification<NewLocationNotification, PLocationNotificationData>, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _locationName;
        BetterOutlinedLabel _locationInfo;
        BetterOutlinedLabel _expInfo;
        VisualElement _locationIcon;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PLocationNotification(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _locationName = contentRoot.Q<BetterOutlinedLabel>("location-name");
            _locationInfo = contentRoot.Q<BetterOutlinedLabel>("location-info");
            _expInfo = contentRoot.Q<BetterOutlinedLabel>("exp-info");
            _locationIcon = contentRoot.Q<VisualElement>("location-icon");
        }

        protected override PLocationNotificationData GetNotificationData() {
            return PresenterDataProvider.locationNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(NewLocationNotification notification) {
            return CommonReferences.Get.AudioConfig.LocationAudio;
        }

        protected override void OnBeforeShow(NewLocationNotification notification) {
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            
            NewLocationNotificationData locationData = notification.locationNotificationData;
            _locationName.text = locationData.discoveryTitle;
            _locationInfo.text = locationData.discoveryMessage;
            _expInfo.text = $"+{locationData.expReward} {LocTerms.ExperienceShort.Translate()}";
            
            if (locationData.iconRef is { IsSet: true }) {
                locationData.iconRef.RegisterAndSetup(this, _locationIcon);
            }
            
            RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Short);
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