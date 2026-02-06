using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [UsesPrefab("TitleScreen/Expansion/" + nameof(VExpansionOverviewUI))]
    public class VExpansionOverviewUI : View<ExpansionOverviewUI>, IAutoFocusBase, IUIAware {
        [SerializeField] VSarrasExpansionCardUI vSarrasExpansionCardUI;
        [SerializeField] VContentExpansionCardUI vContentExpansionCardUI;
        [SerializeField] MenuUIButton previousButton;
        [SerializeField] MenuUIButton nextButton;
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] RectTransform cardsContainer;
        [SerializeField] RectTransform dotsContainer;
        [SerializeField] ExpansionDot expansionDotPrefab;
        [SerializeField] float cardSpacing = 10f;
        [SerializeField] float moveDuration = 1f;
        [SerializeField] float dotsParentOffsetY = 80f;
        
        public VSarrasExpansionCardUI VSarrasExpansionCardUI => vSarrasExpansionCardUI;
        public VContentExpansionCardUI  VContentExpansionCardUI => vContentExpansionCardUI;
        public bool IsValid => this.IsValidForUIHandle();

        Prompts _prompts;
        Tween _moveTween;
        readonly List<VExpansionCardUI> _cards = new();
        readonly List<ExpansionDot> _dots = new();
        int _currentIndex;
        float _cardWidth;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            InitPrompts();
            nextButton.button.OnClick += Next;
            previousButton.button.OnClick += Previous;
            InitializeCarousel().Forget();
        }

        protected override void OnMount() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
        }

        public void AddCard(VExpansionCardUI card) {
            _cards.Add(card);
            var dot = Instantiate(expansionDotPrefab, dotsContainer);
            _dots.Add(dot);
        }

        public void OpenAtIndex(int index, bool initialize = false) {
            if (index == _currentIndex) {
                return;
            }
            
            _currentIndex = math.clamp(index,  0, _cards.Count - 1);
            SnapToIndex(index, initialize);
        }

        void InitPrompts() {
            _prompts = Target.AddElement(new Prompts(null));
            _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Discard), Target, closeButton);
        }

        async UniTaskVoid InitializeCarousel() {
            if (!await AsyncUtil.DelayFrame(Target)) {
                return;
            }

            LayoutCards();
            UpdateCardsVisuals(true);
            OpenAtIndex(Target.InitialCardIndex, true);
            var maxCardHeight = _cards.Max(card => card.RectTransform.rect.height);
            dotsContainer.anchoredPosition = new Vector2(0, -maxCardHeight / 2f) + Vector2.down * dotsParentOffsetY;
        }

        void Next() {
            _currentIndex = (_currentIndex + 1) % _cards.Count;
            SnapToIndex(_currentIndex);
        }

        void Previous() {
            _currentIndex = (_currentIndex - 1 + _cards.Count) % _cards.Count;
            SnapToIndex(_currentIndex);
        }

        void LayoutCards() {
            if (_cards.Count == 0) {
                return;
            }

            float width = _cards[0].RectTransform.rect.width;
            _cardWidth = width + cardSpacing;
            for (int i = 0; i < _cards.Count; i++) {
                float x = i * _cardWidth;
                _cards[i].RectTransform.anchoredPosition = new Vector2(x, 0);
            }
        }

        void SnapToIndex(int index, bool initialize = false) {
            float targetX = -index * _cardWidth;
            if (initialize) {
                cardsContainer.anchoredPosition = new Vector2(targetX, cardsContainer.anchoredPosition.y);
            } else {
                _moveTween.Kill();
                _moveTween = cardsContainer.DOAnchorPosX(targetX, moveDuration).SetUpdate(true);
            }
            
            UpdateCardsVisuals(initialize);
        }

        void UpdateCardsVisuals(bool initialize = false) {
            for (int i = 0; i < _cards.Count; i++) {
                bool isActive = i == _currentIndex;
                _cards[i].Select(isActive, initialize);
                _dots[i].Select(isActive);
            }

            _cards[_currentIndex].RectTransform.SetAsLastSibling();
        }

        public UIResult Handle(UIEvent evt) {
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValue:
                    Next();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValue:
                    Previous();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }

        protected override IBackgroundTask OnDiscard() {
            _prompts.Discard();
            foreach (var card in _cards) {
                card.Discard();
            }

            _cards.Clear();
            return base.OnDiscard();
        }
    }
}