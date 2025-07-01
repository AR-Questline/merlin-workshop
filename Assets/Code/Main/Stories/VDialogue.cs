using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories {
    [UsesPrefab("Story/VDialogue")]
    public class VDialogue : View<Story>, IVStoryPanel, IAutoFocusBase, ISemaphoreObserver, IPromptHost {
        const float ChoiceFragileSemaphoreResetTime1 = 0.1f;
        const float ChoiceFadeDuration = 0.15f;

        [SerializeField] GameObject textBackground;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] CanvasGroup textCanvasGroup;
        [SerializeField] Transform choiceParent;
        [SerializeField] CanvasGroup choiceCanvasGroup;
        [SerializeField] Transform statsParent;
        [SerializeField] Scrollbar choiceScrollbar;
        [SerializeField] VGenericPromptUI skipDialoguePrompt;

        Tween _choiceCanvasGroupTween;
        FragileSemaphore _choiceDefaultSemaphore;
        uint _assetLoadingSemaphore;
        bool _showNamesInDialogues;
        bool _showNamesOutsideDialogues;
        Prompt _skipDialoguePrompt;

        public Transform PromptsHost => transform;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            _choiceDefaultSemaphore = new FragileSemaphore(true, this, ChoiceFragileSemaphoreResetTime1, true);
            Target.AddElement(new StoryOnTop(false));
            textBackground.SetActive(false);
            text.text = string.Empty;
            DiscardPreviousDialogues();

            var prompts = Target.AddElement(new Prompts(this));
            _skipDialoguePrompt = Prompt.VisualOnlyTap(KeyBindings.Gameplay.SkipDialogue, string.Empty);
            prompts.BindPrompt(_skipDialoguePrompt, Target, skipDialoguePrompt);
            HideSkipDialoguePrompt();
            //InitPulsingSequence();

            SubtitlesSetting subtitlesSetting = World.Only<SubtitlesSetting>();
            subtitlesSetting.ListenTo(Setting.Events.SettingChanged, SubtitlesSettingChanged, this);
            SubtitlesSettingChanged(subtitlesSetting);
        }

        // TODO: Make it look better and uncomment
        void InitPulsingSequence() {
            DOTween.Sequence()
                .Append(skipDialoguePrompt.transform.DOScale(Vector3.one * 1.15f, 0.6f).SetEase(Ease.InOutQuad))
                .Append(skipDialoguePrompt.transform.DOScale(Vector3.one * 1f, 1.1f).SetEase(Ease.InOutQuad))
                .SetLoops(-1)
                .SetUpdate(true)
                .Play();
        }

        public void ShowSkipDialoguePrompt() {
            _skipDialoguePrompt.SetVisible(true);
        }
        
        public void HideSkipDialoguePrompt() {
            _skipDialoguePrompt.SetVisible(false);
        }

        void SubtitlesSettingChanged(Model model) {
            SubtitlesSetting subtitlesSetting = (SubtitlesSetting)model;
            textCanvasGroup.alpha = subtitlesSetting.SubsEnabled ? 1 : 0;
            _showNamesInDialogues = subtitlesSetting.AreNamesShownInDialogues;
            _showNamesOutsideDialogues = subtitlesSetting.AreNamesShownOutsideDialogues;
        }

        void Update() {
            _choiceDefaultSemaphore.Update();
        }

        void DiscardPreviousDialogues() {
            bool ShouldDiscard(Story s) =>
                s != Target && s.FocusedLocation == Target.FocusedLocation && s.View<VDialogue>() != null && !Target.IsAnyParent(s);
            foreach (var story in World.All<Story>().Where(ShouldDiscard).ToList()) {
                // One NPC shouldn't keep more than 1 conversation
                story.Discard();
            }
        }

        public void ShowFull() {
            //title.gameObject.SetActive(true);
            choiceParent.gameObject.SetActive(true);
        }
        public void ShowOnlyText() {
            title.gameObject.SetActive(false);
            choiceParent.gameObject.SetActive(false);
        }
        
        public void Clear() {
            _assetLoadingSemaphore = 0;
            ResetChoiceCanvasGroup();
            Target.RemoveElementsOfType<Choice>();
            text.text = string.Empty;
            textBackground.SetActive(false);
            World.Only<Focus>().DeselectAll();
        }

        public void ClearText() {
            text.text = string.Empty;
            textBackground.SetActive(false);
        }

        void ResetChoiceCanvasGroup() {
            choiceCanvasGroup.alpha = 0;
            _choiceCanvasGroupTween.Kill();
        }

        public void ShowText(TextConfig textConfig) {
            var textToDisplay = textConfig.Text;

            if (string.IsNullOrWhiteSpace(textToDisplay)) {
                return;
            }
            
            var dialogueNotificationBuffer = World.Only<DialogueNotificationBuffer>();
            bool actorHasNameToDisplay = textConfig.Actor is { HasName: true, ShowNameInDialogue: true};
            
            if (Target.InvolveHero) {
                dialogueNotificationBuffer.ClearBuffer();
                textBackground.SetActive(true);

                if (_showNamesInDialogues) {
                    string nameToDisplay = actorHasNameToDisplay ? textConfig.Actor.Name : LocTerms.UnknownActor.Translate();
                    text.text = LocTerms.DialogueWithActorName.Translate(textToDisplay, nameToDisplay.FontSemiBold());
                } else {
                    text.text = textToDisplay;
                }
                
                // Change focus to speaking location - might need to be removed and moved somewhere else
                if (textConfig.Location != null) {
                    Target.ChangeFocusedLocation(textConfig.Location);
                }
            } else if (World.Only<SubtitlesSetting>().SubsEnabled) {
                if (_showNamesOutsideDialogues) {
                    string nameToDisplay = actorHasNameToDisplay ? textConfig.Actor.Name : LocTerms.UnknownActor.Translate();
                    textToDisplay = LocTerms.DialogueWithActorName.Translate(textToDisplay, nameToDisplay);
                }

                dialogueNotificationBuffer.RemoveOldNotification(Target, textConfig.Actor);
                var dialogueData = new DialogueData(Target, textConfig.Actor, textToDisplay);
                AdvancedNotificationBuffer.Push<DialogueNotificationBuffer>(new DialogueNotification(dialogueData));
            }
        }

        public void SetTitle(string title) {
            this.title.text = title;
        }

        public void OfferChoice(ChoiceConfig choiceConfig) {
            Target.AddElement(new Choice(choiceConfig, Target));
            _choiceDefaultSemaphore.Set(true);
        }

        public Transform LastChoicesGroup() {
            return choiceParent;
        }

        public Transform StatsPreviewGroup() => statsParent;

        public void SpawnContent(DynamicContent contentElement) { }

        // == Notification
        public void ShowChange(Stat stat, int change) {
            //Awaken.Utility.Debugging.Log.When(Awaken.Utility.Debugging.LogType.Important)?.Error("Show Changed not implemented for VDialogue");
        }

        // == Not Available
        public void SetArt(SpriteReference art) => NotAvailableError("Setting story art");
        public void ShowLastChoice(string textToDisplay, string iconName) => NotAvailableError("Showing last choice");
        public void ToggleBg(bool enabled) => NotAvailableError("Toggling background");
        public void ToggleViewBackground(bool enabled) => NotAvailableError("Toggling View Background");
        public void TogglePrompts(bool promptsEnabled) => NotAvailableError("Toggling prompts");

        static void NotAvailableError(string action) {
            Log.Important?.Error($"{action} is not available in dialogue");
        }

        // === Asset Loading Gate
        public void LockChoiceAssetGate() {
            _assetLoadingSemaphore++;
            ResetChoiceCanvasGroup();
        }

        public void UnlockChoiceAssetGate() {
            _assetLoadingSemaphore--;
            if (_assetLoadingSemaphore == 0) {
                _choiceCanvasGroupTween = choiceCanvasGroup.DOFade(1, ChoiceFadeDuration);
            }
        }

        async UniTaskVoid ScrollbarSet() {
            if (await AsyncUtil.WaitForEndOfFrame(this)) {
                choiceScrollbar.value = 1;
            }
        }

        public void AddLinkSupport() { }

        void ISemaphoreObserver.OnUp() {
            UnlockChoiceAssetGate();
            ScrollbarSet().Forget();
        }
        void ISemaphoreObserver.OnDown() => LockChoiceAssetGate();
    }
}