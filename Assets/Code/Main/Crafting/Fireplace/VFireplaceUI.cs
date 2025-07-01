using System;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Fireplace {
    public class VFireplaceUI : View<FireplaceUI>, IAutoFocusBase, IFocusSource, INewThingContainer {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Transform promptHost;
        [SerializeField] GameObject buttonContent;
        [Title("Description")]
        [SerializeField] CanvasGroup descriptionGroup;
        [SerializeField] TMP_Text descriptionLabel;
        [Title("Buttons")]
        [SerializeField] protected ButtonWithDescription cooking; 
        [SerializeField] protected ButtonWithDescription goToSleep;
        [SerializeField] protected ButtonWithDescription levelUp;
        [Title("Prompts")]
        [SerializeField] VBrightPromptUI closePrompt;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptHost => promptHost;
        public bool ForceFocus => true;
        public virtual Component DefaultFocus => cooking.Button;
        public VBrightPromptUI ClosePrompt => closePrompt;
        public bool ClosePromptActive { get; protected set; } = true;
        
        bool _isDescriptionShown;
        Tween _descriptionTween;

        protected override void OnInitialize() {
            World.Services.Get<NewThingsTracker>().RegisterContainer(this);
            descriptionGroup.alpha = 0;
            Target.ListenTo(Model.Events.AfterChanged, OnTargetChanged, this);
            cooking.RegisterButton(Target.CookAction, LocTerms.Cook.Translate(), LocTerms.CookDescription.Translate(), ShowDescription);
            goToSleep.RegisterButton(Target.GoToSleepAction, LocTerms.Rest.Translate(), LocTerms.RestDescription.Translate(), ShowDescription);
            levelUp.RegisterButton(Target.LevelUpAction, LocTerms.LevelUp.Translate(), LocTerms.LevelUpDescription.Translate(), ShowDescription);
            
            SetVisibility(true);
        }
        
        void OnTargetChanged(Model target) {
            SetVisibility(Target.UiVisible);
        }
        
        void SetVisibility(bool visible) {
            gameObject.SetActiveOptimized(visible);
            canvasGroup.blocksRaycasts = visible;
            buttonContent.SetActive(visible);
            
            if (visible) {
                World.Only<Focus>().Select(DefaultFocus);
            }
        }
        
        protected void ShowDescription(bool isShowed, string description) {
            if (_isDescriptionShown == false) {
                _isDescriptionShown = isShowed;
                _descriptionTween = descriptionGroup.DOFade(isShowed ? 1 : 0, UITweens.FadeDuration);
            }

            if (isShowed) {
                descriptionLabel.text = description;
            }
        }
        
        public event Action onNewThingRefresh;
        
        public bool NewThingBelongsToMe(IModel model) {
            return model is HeroTalentPointsAvailableMarker or HeroStatPointsAvailableMarker or HeroMemoryShardAvailableMarker;
        }
        
        public void RefreshNewThingsContainer() {
            onNewThingRefresh?.Invoke();
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardTween(ref _descriptionTween);
            World.Services.Get<NewThingsTracker>().UnregisterContainer(this);
            return base.OnDiscard();
        }

        [Serializable]
        protected class ButtonWithDescription {
            [SerializeField] ButtonConfig buttonConfig;
            
            public ARButton Button => buttonConfig.button;
            public bool Active => buttonConfig.gameObject.activeSelf;
            
            public void SetActive(bool active) {
                buttonConfig.TrySetActiveOptimized(active);
            }
            
            public void RegisterButton(Action onClick, string title, string description, Action<bool, string> showDescription) {
                buttonConfig.InitializeButton(onClick, title);
                buttonConfig.button.OnHover += state => OnHover(state, description, showDescription);
                buttonConfig.button.OnSelected += state => OnSelect(state, description, showDescription);
            }
            
            void OnHover(bool state, string description, Action<bool, string> showDescription) {
                if (RewiredHelper.IsGamepad) return;
                showDescription(state, description);
            }
            
            void OnSelect(bool state, string description, Action<bool, string> showDescription) {
                if (RewiredHelper.IsGamepad == false) return;
                showDescription(state, description);            
            }
        }
    }
}