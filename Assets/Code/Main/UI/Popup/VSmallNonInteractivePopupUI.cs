using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/" + nameof(VSmallNonInteractivePopupUI))]
    public class VSmallNonInteractivePopupUI : View<PopupUI>, IVPopupUI {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI contentText;
        [SerializeField] Transform mainContent;
        [SerializeField] GameObject bg;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        public void Clear() {
            titleText.text = "";
            contentText.text = "";
        }
        
        public void SetTitle(string title) {
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            titleText.text = title;
        }

        public void ShowText(TextConfig textConfig) {
            contentText.gameObject.SetActive(!string.IsNullOrEmpty(textConfig.Text));
            contentText.text = StoryText.Format(null, textConfig.Text, textConfig.Style);
        }

        public void ShowLastChoice(string textToDisplay, string iconName) { }

        public void ShowChange(Stat changedStat, int change) {
            Color color = Color.Lerp(changedStat.Type.Color, new Color(0.6f, 0.6f, 0.6f), 0.6f);
            string colorString = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            ShowText(TextConfig.WithTextAndStyle($"<color={colorString}><nobr>[{change:+#;-#} {changedStat.Type.IconTag}]</nobr></color>",
                StoryTextStyle.StatChange));
        }

        public void ToggleBg(bool bgEnabled) {
            bg.SetActive(bgEnabled);
        }

        public void SpawnContent(DynamicContent dynamicContent) {
            if (dynamicContent == null) {
                return;
            }

            World.SpawnView(dynamicContent.Element, dynamicContent.ViewContentType, true, true, mainContent);
        }
        
        // == Not Available
        public void SetArt(SpriteReference art) => NotAvailableError("Setting story art");
        public void ToggleViewBackground(bool enabled) => NotAvailableError("Toggling View Background");
        public void TogglePrompts(bool promptsEnabled) => NotAvailableError("Toggling prompts");
        public void OfferChoice(ChoiceConfig choiceConfig) => NotAvailableError("Offering choice");
        public Transform LastChoicesGroup() => throw new System.NotImplementedException();
        public Transform StatsPreviewGroup() => throw new System.NotImplementedException();
        
        public void LockChoiceAssetGate() => NotAvailableError("Locking Choice Asset Gate");
        public void UnlockChoiceAssetGate() => NotAvailableError("Unlocking Choice Asset Gate");
        static void NotAvailableError(string action) {
            Log.Important?.Error($"{action} is not available in non interactive popup");
        }
    }
}