using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    public class VSmallPopupUIBase<TParent> : View<TParent>, IVPopupUI, IAutoFocusBase, IPromptHost where TParent : IModel {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI contentText;
        [SerializeField] Transform promptsHost;
        [SerializeField] Transform mainContent;
        [SerializeField] GameObject bg;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptsHost => promptsHost;

        Prompts _prompts;

        protected override void OnMount() {
            _prompts = new Prompts(this);
            Target.AddElement(_prompts);
        }

        public void Clear() {
            titleText.text = string.Empty;
            contentText.text = string.Empty;
        }

        public void SetArt(SpriteReference art) { }

        public void SetTitle(string title) {
            titleText.SetActiveAndText(string.IsNullOrEmpty(title) == false, title);
        }

        public void ShowText(TextConfig textConfig) {
            var text = StoryText.Format(null, textConfig.Text, textConfig.Style);
            contentText.SetActiveAndText(string.IsNullOrEmpty(textConfig.Text) == false, text);
        }

        public void ShowLastChoice(string textToDisplay, string iconName) {}

        public void ShowChange(Stat changedStat, int change) {
            Color color = Color.Lerp(changedStat.Type.Color, new Color(0.6f, 0.6f, 0.6f), 0.6f);
            string colorString = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            ShowText(TextConfig.WithTextAndStyle($"<color={colorString}><nobr>[{change:+#;-#} {changedStat.Type.IconTag}]</nobr></color>", StoryTextStyle.StatChange));
        }
        
        public virtual void OfferChoice(ChoiceConfig choiceConfig) {
            Prompt prompt = choiceConfig.Prompt();
            _prompts.AddPrompt(prompt, Target, prompt.IsActive, prompt.IsVisibleForController);
        }

        public void ToggleBg(bool bgEnabled) {
            bg.SetActive(bgEnabled);
        }

        public void ToggleViewBackground(bool enabled) {
            throw new System.NotImplementedException();
        }

        public void TogglePrompts(bool promptsEnabled) {
            promptsHost.parent.gameObject.SetActive(promptsEnabled);
        }

        public Transform LastChoicesGroup() {
            return promptsHost;
        }
        
        public Transform StatsPreviewGroup() {
            throw new System.NotImplementedException();
        }

        public void SpawnContent(DynamicContent dynamicContent) {
            if (dynamicContent == null) {
                return;
            }
            
            World.SpawnView(dynamicContent.Element, dynamicContent.ViewContentType, true, true, mainContent);
        }

        public void LockChoiceAssetGate() { }
        public void UnlockChoiceAssetGate() { }
    }
}