using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Choices.ChoicePreviews;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Choices {
    /// <summary>
    /// Story choice view, show proper button
    /// </summary>
    [UsesPrefab("Story/" + nameof(VChoice))]
    public class VChoice : View<Choice>, IHoverableView, IFocusSource, INaviOverride, IWithTooltip, IAssetLoadingGate {
        uint _assetLoadingSemaphore;
        bool _hasBeenLocked;
        SubtitlesSetting _subtitlesSetting;
        
        [SerializeField] SpriteAtlas buttonIcons;
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] ARButton button;
        [SerializeField] GameObject iconGameObject;
        [SerializeField] GameObject enableBackground;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI mainText;
        
        // === Bridge to showing additional data
        public string EffectAndCost => Target.EffectAndCost;

        View IAssetLoadingGate.OwnerView => this;

        string AggregatedEffects => string.Join("\n", Effects.Where(eff => !string.IsNullOrWhiteSpace(eff)));
        IEnumerable<string> Effects {
            get {
                yield return Target.EffectAndCost;
            }
        }

        // Force Focus if something else than other VChoice is selected
        public bool ForceFocus => World.Only<Focus>().Focused?.GetComponentInParent<VChoice>() == null;
        public Component DefaultFocus => button;

        public override Transform DetermineHost() => Target.Story.LastChoicesGroup();

        protected override void OnInitialize() {
            _subtitlesSetting = World.Only<SubtitlesSetting>();
            SetButtonValues();
            buttonConfig.InitializeButton();
            // add 0.5s delay on callback, so user doesn't click it unintentionally
            DOVirtual.DelayedCall(0.5f, () => button.OnClick += Target.Callback);
            button.OnHover += OnChoiceHovered;

            if (_assetLoadingSemaphore > 0 && !_hasBeenLocked) {
                _hasBeenLocked = true;
                Target.Story.LockChoiceAssetGate();
            }
        }

        void OnChoiceHovered(bool isHovered) {
            if (Target.HoverInfos.Count == 0) {
                return;
            }

            if (isHovered) {
                Target.Element<ChoicePreview>().View<VChoicePreview>().OnHoverEnter();
            } else {
                Target.Element<ChoicePreview>().View<VChoicePreview>().OnHoverExit();
            }
        }

        void SetButtonValues() {
            Sprite iconSprite;
            iconGameObject.SetActive(false);
            if (Target.IconReference?.IsSet ?? false) { Target.IconReference.RegisterAndSetup(this, icon, (_, _) => {
                    OnIconLoaded();
                });
            } else if ((iconSprite = buttonIcons.GetSprite(Target.IconName)) != null) {
                icon.sprite = iconSprite;
                OnIconLoaded();
            }
            
            button.Interactable = Target.Enable;
            enableBackground.SetActiveOptimized(Target.Enable);
            mainText.text = Target.ButtonText.FormatSprite();
            mainText.color = ColorChoice();
            PrependAggregatedEffects();
            return;

            void OnIconLoaded() {
                iconGameObject.SetActive(true);
                icon.color = Target.Enable ? ARColor.MainWhite : ARColor.DarkerGrey;
            }
        }

        Color ColorChoice() {
            if (!Target.Enable) {
                return ARColor.ChoiceDisableText;
            }

            if (Target.IsMainChoice || Target.IsHighlighted) {
                return ARColor.MainAccent;
            }

            return World.Only<SubtitlesSetting>().ActiveColor;
        }
        
        void PrependAggregatedEffects() {
            if (string.IsNullOrWhiteSpace(AggregatedEffects)) {
                return;
            }
            
            mainText.text = $"{mainText.text}\n{FormatAggregatedEffects()}";
        }

        string FormatAggregatedEffects() {
            string effects = AggregatedEffects.Italic().ColoredText(_subtitlesSetting.ActiveColor);
            string[] split = effects.Split(':');
            return split.Length == 2 ? $"{split[0]}:{split[1].FontSemiBold()}" : effects;
        }

        // === Hover
        public UIResult Handle(UIEvent evt) {
            return UIResult.Ignore;
        }

        public UIResult Navigate(UINaviAction direction) {
            if (direction.direction == NaviDirection.Down) {
                return Select(NextChoice(Target.CurrentIndex + 1));
            }

            if (direction.direction == NaviDirection.Up) {
                return Select(PreviousChoice(Target.CurrentIndex - 1));
            }

            return UIResult.Ignore;
        }

        Choice NextChoice(int nextIndex) {
            var possibleChoice = nextIndex < Target.Choices.Length ? Target.Choices[nextIndex] : Target.Choices.First(choice => choice.Enable);
            return possibleChoice.Enable ? possibleChoice : NextChoice(nextIndex + 1);
        }
        
        Choice PreviousChoice(int previousIndex) {
            var possibleChoice = previousIndex < 0 ? Target.Choices.Last(choice => choice.Enable) : Target.Choices[previousIndex];
            return possibleChoice.Enable ? possibleChoice : PreviousChoice(previousIndex - 1);
        }

        static UIResult Select(Choice choice) {
            World.Only<Focus>().Select(choice.View<VChoice>().button);
            return UIResult.Accept;
        }

        public TooltipConstructor TooltipConstructor => Target.Tooltip;

        public bool TryLock() {
            if (++_assetLoadingSemaphore == 1 && Target != null) {
                _hasBeenLocked = true;
                Target.Story.LockChoiceAssetGate();
            }

            return true;
        }

        public void Unlock() {
            if (--_assetLoadingSemaphore == 0 && Target != null) {
                _hasBeenLocked = false;
                Target.Story.UnlockChoiceAssetGate();
            }
        }
    }
}
