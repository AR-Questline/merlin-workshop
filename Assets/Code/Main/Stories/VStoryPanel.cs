using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories
{
    public class VStoryPanel : View<Story>, IVStoryPanel, IFocusSource {
        const float ChoiceScaleFactor = 0.7f;
        
        public Transform storyParent;
        public ArtAnimator artAnimator;
        public TextMeshProUGUI storyTitleText;
        public Image blackBg;
        public GameObject[] objectsToDisableOnBgToggle = Array.Empty<GameObject>();
        public CanvasGroup content;
        public Vector3 choiceScale;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;
        
        StoryFancyPanel _storyFancyPanel;
        Transform _choicesLayout;
        Tween _introTween;
        View _heroBars;

        // === Initialization

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            Clear();
            LoadAllUiPrefabs();
            artAnimator.Setup();
            AnimateSlideIn();
            Target.AddElement(new StoryOnTop());
            
            World.Only<Focus>().SwitchToFocusBase(storyParent);
        }
        
        public void AnimateSlideIn() {
            RectTransform storyRectTransform = transform.GrabChild<RectTransform>("StoryPanel");
            var destination = storyRectTransform.anchoredPosition;
            storyRectTransform.anchoredPosition = destination + new Vector2(storyRectTransform.sizeDelta.x, 0);
            DOTween.To(() => storyRectTransform.anchoredPosition, x => storyRectTransform.anchoredPosition = x, destination, 0.2f);
            content.alpha = 0;
            _introTween = DOTween.To(() => content.alpha, x => content.alpha = x, 1, 0.7f);
        }

        public void FadeOut() { 
            DOTween.To(() => content.alpha, x => content.alpha = x, 0, 0.7f);
        }

        // === UI prefab cache
        Dictionary<string, GameObject> _uiPrefabs;

        void LoadAllUiPrefabs() {
            _uiPrefabs = new Dictionary<string, GameObject>();
            foreach (var gob in Resources.LoadAll<GameObject>("Prefabs/StoryUI")) {
                _uiPrefabs[gob.name] = gob;
            }
        }
        
        GameObject SpawnUi(string prefabName, Vector3 prefabScale) {
            var prefab = _uiPrefabs[prefabName];
            GameObject instance = Instantiate(prefab, storyParent);
            instance.transform.localScale = prefabScale;
            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
            CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            DOTween.To(() => canvasGroup.alpha, v => canvasGroup.alpha = v, 1f, 1.2f);
            InitializeTexts();
            return instance;
        }

        // === UI interactions (mirrors IStoryApi, since some calls are delegated here)
        
        public void Clear() {
            Target.RemoveElementsOfType<Choice>();
            GameObjects.DestroyAllChildrenSafely(storyParent);
            _choicesLayout = null;
            _storyFancyPanel = null;
            World.Only<Focus>().DeselectAll();
        }

        public void SetArt(SpriteReference art) {
            if (_introTween.active) {
                artAnimator.SetArt(art, false);
            } else {
                artAnimator.SetArt(art);
            }
        }

        public void SetTitle(string title) {
            storyTitleText.text = title;
        }

        public void ClearText() { }

        public void ShowText(TextConfig textConfig) {
            if (textConfig.Icon is { IsSet: true }) {
                ShowTextWithIcon(textConfig.Text, textConfig.Icon, textConfig.Style, textConfig.HasLink);
            } else {
                ShowText(textConfig.Text, textConfig.Style, textConfig.HasLink);
            }
        }

        public void ShowText(string textToDisplay, StoryTextStyle style, bool hasLink) {
            TextMeshProUGUI component = SpawnUi("StoryText", Vector3.one).GetComponent<TextMeshProUGUI>();
            component.text = StoryText.Format(Target, textToDisplay, style);
            GetComponent<VCAccessibility>()?.AddTextMeshPro(component);
            if (hasLink) {
                StoryUtils.AddTextLinkHandler(this, component.gameObject);
            }
        }

        public void ShowTextWithIcon(string textToDisplay, ShareableSpriteReference icon, StoryTextStyle style, bool hasLink) {
            StoryTextWithIcon storyStoryText = SpawnUi("StoryTextWithIcon", Vector3.one).GetComponent<StoryTextWithIcon>();
            storyStoryText.icon.TrySetActiveOptimized(false);
            storyStoryText.text.text = StoryText.Format(Target, textToDisplay, style);
            icon.RegisterAndSetup(this, storyStoryText.icon, (img, sprite) => {
                img.TrySetActiveOptimized(true);
            });
            GetComponent<VCAccessibility>()?.GetInstanceID();
            if (hasLink) {
                StoryUtils.AddTextLinkHandler(this, storyStoryText.gameObject);
            }
        }

        public void ShowLastChoice(string textToDisplay, string iconName) {
            StoryLastChoice storyLastChoice = SpawnUi("StoryLastChoice", Vector3.one * ChoiceScaleFactor).GetComponent<StoryLastChoice>();
            storyLastChoice.text.text = StoryText.Format(Target, textToDisplay, StoryTextStyle.Plain);
            var iconSprite = storyLastChoice.buttonIcons.GetSprite(iconName);
            if (iconSprite != null) {
                storyLastChoice.icon.sprite = iconSprite;
            }
            else {
                storyLastChoice.iconHolder.SetActive(false);
            }
        }

        public void ShowChange(Stat changedStat, int change) {
            return; //not supported in Conquest story panel
            string text = $"{Mathf.Abs(change)} {changedStat.Type.IconTag}".FormatSprite();
            AddToFancyPanel(text, change > 0 ? FancyPanelType.Good : FancyPanelType.Bad);
        }

        public void ShowItemChange(ItemTemplate item, int quantity) {
            string tooltip = item.ItemName;
            string text = ($"{Mathf.Abs(quantity)} {{item}}[{item.ItemName}]").AddTooltip(tooltip).FormatSprite();
            ShowFancyPanel(fancyPanelType: quantity > 0 ? FancyPanelType.Good : FancyPanelType.Bad, content: text);
            _storyFancyPanel = null;
        }

        public void OfferChoice(ChoiceConfig choiceConfig) {
            Choice choice = new(choiceConfig, Target);
            Target.AddElement(choice);
        }

        public void ToggleBg(bool bgEnabled) {
            DOTween.ToAlpha(() => blackBg.color, a => blackBg.color = a, bgEnabled ? 1 : 0, 1.5f);
        }

        public void ToggleViewBackground(bool bgEnabled) {
            objectsToDisableOnBgToggle.ForEach(g => g.SetActive(bgEnabled));
        }

        public void TogglePrompts(bool promptsEnabled) {
            Log.Important?.Error("Toggling prompts is not available in story panel");
        }

        public void ShowFancyPanel(string mainText = "", FancyPanelType fancyPanelType = null, string content = "", string additionalInfo = "") {
            _storyFancyPanel = SpawnUi("StoryFancyPanel", Vector3.one).GetComponent<StoryFancyPanel>();
            mainText = StoryText.FormatVariables(Target, mainText);
            content = StoryText.FormatVariables(Target, content);
            
            // Default text for new fancy panel if none is provided
            if (string.IsNullOrWhiteSpace(mainText)) {
                if (fancyPanelType == FancyPanelType.Good) {
                    _storyFancyPanel.background.sprite = _storyFancyPanel.icons.GetSprite("blend_gold");
                    _storyFancyPanel.textWithStats.text = $"{LocTerms.YouGot.Translate()}:";
                } else if (fancyPanelType == FancyPanelType.Bad) {
                    _storyFancyPanel.background.sprite = _storyFancyPanel.icons.GetSprite("blend_red");
                    _storyFancyPanel.textWithStats.text = $"{LocTerms.YouLost.Translate()}:";
                }
            } else {
                _storyFancyPanel.textWithStats.text = mainText;
                _storyFancyPanel.textMiddle.text = mainText;
            }
            
            _storyFancyPanel.FancyPanelType = fancyPanelType;
            _storyFancyPanel.textStats.text = content;
            _storyFancyPanel.textAdditional.text = additionalInfo;
            _storyFancyPanel.contentTextOnly.SetActive(string.IsNullOrWhiteSpace(content));
            _storyFancyPanel.contentWithStats.SetActive(!string.IsNullOrWhiteSpace(content));
            VCAccessibility accessibility = GetComponent<VCAccessibility>();
            
            if (accessibility != null) {
                accessibility.AddTextMeshPro(_storyFancyPanel.textAdditional);
                accessibility.AddTextMeshPro(_storyFancyPanel.textMiddle);
                accessibility.AddTextMeshPro(_storyFancyPanel.textStats);
                accessibility.AddTextMeshPro(_storyFancyPanel.textWithStats);
            }
        }

        public void AddToFancyPanel(string text, FancyPanelType fancyPanelType) {
            if (_storyFancyPanel != null && _storyFancyPanel.FancyPanelType == fancyPanelType) {
                _storyFancyPanel.textStats.text += $" {text}";
            } else {
                ShowFancyPanel(fancyPanelType: fancyPanelType, content: text);
            }
        }

        public Transform LastChoicesGroup() {
            GameObject lastChild = storyParent.LastChild();
            if (_choicesLayout == null || _choicesLayout.gameObject != lastChild) {
                _choicesLayout = SpawnUi("StoryChoices", Vector3.one * ChoiceScaleFactor).transform;
            }
            return _choicesLayout;
        }

        public Transform StatsPreviewGroup() {
            return null;
        }

        public void SpawnContent(DynamicContent dynamicContent) {}

        public void LockChoiceAssetGate() { }

        public void UnlockChoiceAssetGate() { }

        protected override IBackgroundTask OnDiscard() {
            if (_heroBars != null) {
                _heroBars.Discard();
            }

            Resources.UnloadUnusedAssets();
            return base.OnDiscard();
        }
    }
}
