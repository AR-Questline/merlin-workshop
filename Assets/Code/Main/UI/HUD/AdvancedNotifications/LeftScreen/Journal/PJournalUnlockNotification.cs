using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.Main.UIToolkit.PresenterData.Notifications;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Journal {
    public class PJournalUnlockNotification : PAdvancedNotification<JournalUnlockNotification, PJournalUnlockNotificationData>, IPromptListener, IPresenterWithAccessibilityBackground {
        BetterOutlinedLabel _newEntryText;
        BetterOutlinedLabel _unlockText;
        VisualPresenterKeyIcon _keyIcon;
        Prompt _openJournalPrompt;
        JournalSubTabType _journalTabType;
        string _journalEntryName;
        
        VisualElement IPresenterWithAccessibilityBackground.Host => Content;
        
        public PJournalUnlockNotification(VisualElement parent) : base(parent) { }
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _newEntryText = contentRoot.Q<BetterOutlinedLabel>("new-entry-text");
            _newEntryText.text = LocTerms.NewJournalEntry.Translate();
            _unlockText = contentRoot.Q<BetterOutlinedLabel>("unlock-text");
            _keyIcon = new VisualPresenterKeyIcon(contentRoot.Q<VisualElement>("key-icon"));
            
            var prompts = TargetModel.AddElement(new Prompts(null));
            _openJournalPrompt = Prompt.Tap(KeyBindings.UI.HUD.OpenInventoryItemRead, LocTerms.Open.Translate(), () => OpenJournal(_journalEntryName, _journalTabType));
            World.BindPresenter(TargetModel, _keyIcon, () => {
                _keyIcon.Setup(_openJournalPrompt);
                prompts.AddPrompt(_openJournalPrompt, TargetModel, this, false, false);
            });
        }

        protected override PJournalUnlockNotificationData GetNotificationData() {
            return PresenterDataProvider.journalUnlockNotificationData;
        }

        protected override NotificationSoundEvent GetNotificationSound(JournalUnlockNotification notification) {
            return CommonReferences.Get.AudioConfig.JournalAudio;
        }

        protected override void OnBeforeShow(JournalUnlockNotification notification){
            Content.transform.position = Vector3.right * Data.InitialXOffset;

            _journalEntryName = notification.journalEntry;
            _unlockText.text = _journalEntryName;
            _journalTabType = notification.journalTabType;
            
            RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Long);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            _openJournalPrompt.SetupState(true, true);
        }

        protected override void OnAfterHide() {
            _openJournalPrompt.SetupState(false, false);
        }

        protected override Sequence ShowSequence() {
            return DOTween.Sequence().SetUpdate(IsIndependentUpdate).SetDelay(Data.ShowDelayDuration)
                .Append(Content.DoFade(1f, Data.FadeDuration))
                .Join(Content.DoMove(Vector3.zero, Data.MoveDuration))
                .AppendInterval(Data.VisibilityDuration)
                .Append(Content.DoFade(0f, Data.FadeDuration));
        }

        static void OpenJournal(string entryName, JournalSubTabType tabType) {
            CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Journal, afterViewSpawnedCallback: () => {
                World.Only<JournalUI>().OverrideTabAndEntry(entryName, tabType);
            });
        }

        // --- IPromptListener
        public void SetName(string name) { }

        public void SetActive(bool active) { }

        public void SetVisible(bool visible) {
            if (visible) {
                _keyIcon.Content.ShowAndSetActiveOptimized();
            } else {
                _keyIcon.Content.HideAndSetActiveOptimized();
            }
        }
    }
}