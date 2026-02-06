using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    public abstract class VExpansionCardUI : View<ExpansionEntryUI> {
        [SerializeField] VGenericPromptUI openStorePrompt;
        [SerializeField] ARButton button;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI expansionTypeText;
        [SerializeField] TextMeshProUGUI expansionTitleText;
        [SerializeField] TextMeshProUGUI timeToReleaseText;
        [SerializeField] TextMeshProUGUI expansionDescriptionText;
        [SerializeField] float inactiveScale = 0.9f;
        [SerializeField] float inactiveAlpha = 0.5f;
        [SerializeField] float moveDuration = 1f;
        [SerializeField] float quickFadeDuration = 0.05f;
        
        Prompts _prompts;
        Prompt _openStorePrompt;
        Sequence _moveSequence;
        
        public RectTransform RectTransform { get; private set; }

        protected abstract DlcId DlcId { get; }
        
        CanvasGroup CanvasGroup => canvasGroup;
        string Type => Target.ExpansionEntryData.type;
        string Title => Target.ExpansionEntryData.title;
        string Description => Target.ExpansionEntryData.description;
        DateTime ReleaseDate => Target.ExpansionEntryData.releaseDate;
        
        protected override void OnInitialize() {
            button.OnClick += () => World.Only<ExpansionOverviewUI>().View<VExpansionOverviewUI>().OpenAtIndex(Target.ExpansionIndex);
            RectTransform = GetComponent<RectTransform>();
            expansionTypeText.SetText(Type);
            expansionTitleText.SetText(Title);
            expansionDescriptionText.SetText(Description);
            timeToReleaseText.SetText(ExpansionUtils.GetTimeToReleaseText(ReleaseDate));
            _prompts = Target.AddElement(new Prompts(null));
            _openStorePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Expansion.OpenStore, LocTerms.ExpansionOpenStore.Translate(), OpenStore), Target, openStorePrompt, !PlatformUtils.IsPS5, !PlatformUtils.IsPS5);
        }

        public void Select(bool state, bool initialize = false) {
            bool isReleased = ExpansionUtils.GetDaysLeft(ReleaseDate) < 0;
            _openStorePrompt.SetActive(state);
            timeToReleaseText.TrySetActiveOptimized(!isReleased);
            Move(state, initialize);
        }

        void OpenStore() {
            if (ReleaseDate < DateTime.Now) {
                SocialService.Get.ShowPurchaseDialog(DlcId)
                         .ContinueWith(_ => SocialService.Get.RecollectAddOns())
                         .Forget();
            } else {
                SocialService.Get.ShowStorePage(DlcId).Forget();
            }
        }

        void Move(bool state, bool initialize = false) {
            Vector3 targetScale = Vector3.one * (state ? 1f : inactiveScale);
            float targetAlpha = state ? 1f : inactiveAlpha;

            if (initialize) {
                RectTransform.localScale = targetScale;
                CanvasGroup.alpha = targetAlpha;
                return;
            }
            
            _moveSequence.Kill();
            _moveSequence = DOTween.Sequence().SetUpdate(true)
                .Join(RectTransform.DOScale(targetScale, moveDuration))
                .Join(CanvasGroup.DOFade(targetAlpha, state ? quickFadeDuration : moveDuration));
        }
        
        protected override IBackgroundTask OnDiscard() {
            _prompts.Discard();
            return base.OnDiscard();
        }
    }
}