using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public class VCCSlider : View<CCSlider>, IUIAware, IVCCFocusablePart {
        [SerializeField] ARMultiOptions multiOptions;
        [SerializeField] TextMeshProUGUI description;
        
        AlwaysPresentHandlers _alwaysPresentHandlers;
        
        protected override void OnInitialize() {
            multiOptions.Initialize(Target.NameOfValue(Target.SavedValue), Target.Decrease, Target.Increase);
            Target.CharacterCreator.ListenTo(CharacterCreator.Events.AppearanceChanged, _ => Refresh(), this);
            multiOptions.OnHovered += OnHover;
            Refresh();
        }

        void Refresh() {
            var value = Target.SavedValue;
            multiOptions.NameLabel.text = Target.NameOfValue(value);

            bool canDecrease = Target.CanDecrease();
            multiOptions.PrevButton.interactable = canDecrease;

            bool canIncrease = Target.CanIncrease();
            multiOptions.NextButton.interactable = canIncrease;
            
            if (Target.HasDescription) {
                description.SetText(Target.DescriptionOfValue(value)); //works for VCCSliderWithDescription prefab
            }
        }

        void OnHover(bool hovered) {
            if (hovered) {
                _alwaysPresentHandlers ??= World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, multiOptions, Target));
                Target.CharacterCreator.SetPromptInvoker(Target);
            }
            else {
                _alwaysPresentHandlers?.Discard();
                _alwaysPresentHandlers = null;
            }
        }

        public void ReceiveFocusFromTop(float horizontalPercent) => ReceiveFocus();
        public void ReceiveFocusFromBottom(float horizontalPercent) => ReceiveFocus();
        void ReceiveFocus() {
            World.Only<Focus>().Select(this);
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UINaviAction naviAction) {
                if (naviAction.direction == NaviDirection.Up) {
                    Target.FocusAbove();
                    return UIResult.Accept;
                }

                if (naviAction.direction == NaviDirection.Down) {
                    Target.FocusBelow();
                    return UIResult.Accept;
                }
            }
            
            return UIResult.Ignore;
        }
    }
}