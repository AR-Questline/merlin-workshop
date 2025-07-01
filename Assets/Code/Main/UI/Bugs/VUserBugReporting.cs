using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.UI.GamepadKeyboard;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Bugs {
    [UsesPrefab("Settings/VUserBugReporting")]
    public class VUserBugReporting : View<UserBugReporting>, IUIAware, IAutoFocusBase, IFocusSource, IPromptHost {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] GameObject progressPanel;
        [SerializeField] TMP_InputField title;
        [SerializeField] TMP_InputField description;
        [SerializeField] TextMeshProUGUI titleFiledInfo;
        [SerializeField] TextMeshProUGUI descriptionFiledInfo;
        [SerializeField] VGenericPromptUI applyPrompt;
        [SerializeField] Transform keyboardParent;
        [SerializeField] ARButton keyboardTitleButton;
        [SerializeField] ARButton keyboardDescButton;
        [SerializeField] TextMeshProUGUI progressText;
        [SerializeField] Transform promptsHost;
        
        Sequence _sequence;
        Prompt _acceptPrompt;

        public TextMeshProUGUI ProgressText => progressText;
        public Transform PromptsHost => promptsHost;
        public bool ForceFocus => true;
        public Component DefaultFocus => keyboardTitleButton;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
            keyboardTitleButton.OnClick += () => ShowKeyboard(keyboardTitleButton, title);
            keyboardDescButton.OnClick += () => ShowKeyboard(keyboardDescButton, description);
        }

        protected override void OnMount() {
            InitializePrompts();
        }
        
        void InitializePrompts() {
            var prompts = new Prompts(this);
            Target.AddElement(prompts);
            
            var cancelPrompt = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Close, Prompt.Position.Last);
            prompts.AddPrompt(cancelPrompt, Target);
            _acceptPrompt = Prompt.Tap(KeyBindings.UI.BugReport.ConfirmAndSend, LocTerms.Confirm.Translate(), SendBugReport);
            prompts.BindPrompt(_acceptPrompt, Target, applyPrompt);
        }

        void Update() {
            _acceptPrompt.SetupState(true, !description.isFocused && !title.isFocused);
        }

        public void Hide() {
            canvasGroup.alpha = 0;
        }

        public void Show() {
            canvasGroup.alpha = 1;
        }

        // === Execution
        void SendBugReport() {
            _sequence?.Complete();
            _sequence = DOTween.Sequence().SetUpdate(true);
            bool textsFiled = true;
            if (string.IsNullOrEmpty(title.text)) {
                _sequence.Join(DOTween.ToAlpha(() => titleFiledInfo.color, a => titleFiledInfo.color = a, 1f, 0.25f));
                textsFiled = false;
            }

            if (string.IsNullOrWhiteSpace(description.text)) {
                _sequence.Join(DOTween.ToAlpha(() => descriptionFiledInfo.color, a => descriptionFiledInfo.color = a, 1f, 0.25f));
                textsFiled = false;
            }

            if (!textsFiled) {
                _sequence.AppendInterval(3f);
                _sequence.Append(DOTween.ToAlpha(() => titleFiledInfo.color, a => titleFiledInfo.color = a, 0f, 0.25f));
                _sequence.Join(DOTween.ToAlpha(() => descriptionFiledInfo.color, a => descriptionFiledInfo.color = a, 0f, 0.25f));
            } else {
                Log.Marking?.Warning($"Player sending bug report: {title.text}, {description.text}");
                Target.CreateUserReport(title.text, description.text).Forget();
            }
        }
        
        public void ShowProgressPanel() {
            progressPanel.SetActive(true);
        }
        
        public void ShowResult(bool success) {
            success = true; // Disabled fail popup
            progressPanel.SetActive(false);
            PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), success ? LocTerms.BugReportingSend.Translate() : LocTerms.BugReportingFailed.Translate(), LocTerms.SettingsBugReporting.Translate(), () => {
                if (success) {
                    AutoBugReporting.ReConfigure();
                    Target.Discard();
                }
            });
        }
        
        // === Gamepad Keyboard
        void ShowKeyboard(ARButton button, TMP_InputField inputField) {
            if (RewiredHelper.IsGamepad && !Target.HasElement<Keyboard>()) {
                var keyboard = new Keyboard(keyboardParent, inputField);
                Target.AddElement(keyboard);
                keyboard.ListenTo(Model.Events.AfterDiscarded, () => HideKeyboard(button), this);
                keyboardTitleButton.Interactable = false;
                keyboardDescButton.Interactable = false;
            }
        }

        void HideKeyboard(ARButton button) {
            keyboardTitleButton.Interactable = true;
            keyboardDescButton.Interactable = true;
            World.Only<Focus>().Select(button);
        }
        
        // === Handlers
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyEvent key && AllowedKeys.Contains(key.Key)) {
                // if (RewiredHelper.Player.GetButtonDown(KeyBindings.UI.Generic.Cancel)) {
                //     IClosable closable = World.LastOrNull<IClosable>();
                //     // closable is closed only if it is active shortcut
                //     if (closable?.IsActive() ?? false) {
                //         closable.Close();
                //     }
                // }

                return UIResult.Ignore;
            }
            
            return UIResult.Ignore;
        }
        
        static readonly KeyCode[] AllowedKeys = {
            KeyCode.Backspace,
            KeyCode.Delete,
            KeyCode.Return,
            KeyCode.DownArrow,
            KeyCode.UpArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.PageDown,
            KeyCode.PageUp,
            KeyCode.Escape
        };
    }
}
