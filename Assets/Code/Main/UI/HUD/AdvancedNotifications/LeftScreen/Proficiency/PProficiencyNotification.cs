using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility.UI;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency {
    public class PProficiencyNotification : PAdvancedNotification<ProficiencyNotification, PProficiencyNotificationData>, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _proficiencyName;
        BetterOutlinedLabel _proficiencyLevel;
        VisualFillBar _levelFillBar;
        VisualElement _proficiencyIcon;
        readonly Vector3 _progressScale = new(0f, 1f, 1f);

        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PProficiencyNotification(VisualElement parent) : base(parent) { }
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _proficiencyName = contentRoot.Q<BetterOutlinedLabel>("proficiency-name");
            _proficiencyLevel = contentRoot.Q<BetterOutlinedLabel>("proficiency-level");
            _levelFillBar = new VisualFillBar(contentRoot.Q<VisualElement>("level-fill-bar")).Set(VisualFillBarType.Horizontal, _progressScale);
            _proficiencyIcon = contentRoot.Q<VisualElement>("proficiency-icon");
        }

        protected override PProficiencyNotificationData GetNotificationData() {
            return PresenterDataProvider.proficiencyNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(ProficiencyNotification notification) {
            return CommonReferences.Get.AudioConfig.ProficiencyAudio;
        }

        protected override void OnBeforeShow(ProficiencyNotification notification) {
            Content.Hide();
            Content.transform.position = Vector3.right * Data.InitialXOffset;
            _levelFillBar.Reset();
            _proficiencyLevel.Hide();
            _proficiencyLevel.transform.scale = Vector3.one * Data.LevelInitialScale;
            
            ProficiencyData proficiencyData = notification.proficiencyData;
            _proficiencyName.text = proficiencyData.skillName;
            _proficiencyLevel.text = proficiencyData.newSkillLevel.ToString();

            if (proficiencyData.proficiencyIcon is {IsSet: true} icon) {
                icon.RegisterAndSetup(this, _proficiencyIcon);
            }
            
            RewiredHelper.VibrateHighFreq(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
        }

        protected override Sequence ShowSequence() {
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .Join(Content.DoMove(Vector3.zero, Data.MoveDuration))
                .Append(DOVirtual.Float(0f, 1f, Data.ProgressBarDuration, x => _levelFillBar.Fill(x)))
                .Append(_proficiencyLevel.DoScale(Vector3.one, Data.LevelScaleDuration))
                .Join(_proficiencyLevel.DoFade(1f, Data.LevelFadeDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration));
        }
    }
}