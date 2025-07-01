using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    public abstract class VPopupUI<TParent> : View<TParent>, IVPopupUI, IAutoFocusBase where TParent : IModel {
        public TextMeshProUGUI titleText;
        public ArtAnimator artAnimator;
        public GameObject textPrefab; 
        public GameObject buttonPrefab;
        public Transform parent;
        public Transform buttonParent;
        public Transform contentRoot;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            artAnimator.Setup();
            contentRoot.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            DOTween.To(() => contentRoot.localScale, x => contentRoot.localScale = x, Vector3.one, 0.2f).SetUpdate(true);
        }

        // === Story View implementation

        public virtual void Clear() {
            GameObjects.DestroyAllChildrenSafely(parent, textPrefab);
            GameObjects.DestroyAllChildrenSafely(buttonParent, buttonPrefab);
        }

        public virtual void SetArt(SpriteReference art) {
            artAnimator.SetArt(art);
        }

        public virtual void SetTitle(string title) {
            titleText.text = title;
        }

        public virtual void ShowText(TextConfig textConfig) {
            GameObject obj = Instantiate(textPrefab, parent, false);
            obj.SetActive(true);
            
            TextMeshProUGUI component = obj.GetComponentInChildren<TextMeshProUGUI>();
            component.text = StoryText.Format(Target as Story, textConfig.Text, textConfig.Style);
            if (textConfig.HasLink) {
                StoryUtils.AddTextLinkHandler(this, component.gameObject);
            }
        }
        public void ShowLastChoice(string textToDisplay, string iconName) {}
        public virtual void OfferChoice(ChoiceConfig choiceConfig) {
            GameObject obj = Instantiate(buttonPrefab, buttonParent, false);
            obj.SetActive(true);
            
            ARButton button = obj.GetComponentInChildren<ARButton>();
            TextMeshProUGUI textMesh = obj.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.text = choiceConfig.DisplayText();
            button.OnClick += choiceConfig.Callback() ?? (() => (Target as Story)?.JumpTo(choiceConfig.TargetChapter()));
        }

        public void ToggleBg(bool bgEnabled) {
            throw new System.NotImplementedException();
        }

        public void ToggleViewBackground(bool enabled) {
            throw new System.NotImplementedException();
        }

        public void TogglePrompts(bool promptsEnabled) {
            throw new System.NotImplementedException();
        }

        public virtual Transform LastChoicesGroup() {
            return buttonParent;
        }
        
        public virtual Transform StatsPreviewGroup() {
            throw new System.NotImplementedException();
        }

        public void SpawnContent(DynamicContent contentElement) { }

        public virtual void ShowChange(Stat changedStat, int change) {
            Color color = Color.Lerp(changedStat.Type.Color, new Color(0.6f, 0.6f, 0.6f), 0.6f);
            string colorString = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            ShowText(TextConfig.WithTextAndStyle($"<color={colorString}><nobr>[{change:+#;-#} {changedStat.Type.IconTag}]</nobr></color>", StoryTextStyle.StatChange));
        }

        public void LockChoiceAssetGate() { }
        public void UnlockChoiceAssetGate() { }
    }
}
