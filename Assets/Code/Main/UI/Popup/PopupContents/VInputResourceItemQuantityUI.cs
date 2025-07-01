using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    [UsesPrefab("Story/PopupContents/" + nameof(VInputResourceItemQuantityUI))]
    public class VInputResourceItemQuantityUI : VInputItemQuantityUI {
        protected override void OnInitialize() {
            base.OnInitialize();
            ClearSlider();
        }

        public void SetupSlider(int lowerBound, int upperBound, bool interactable) {
            gameObject.SetActive(true);
            slider.interactable = interactable;
            buttonIncrease.Interactable = interactable;
            buttonDecrease.Interactable = interactable;
            if (lowerBound > upperBound || (lowerBound == 0 && upperBound == 0)) {
                ClearSlider();
                return;
            }
            
            Target.LowerBound = lowerBound;
            Target.UpperBound = upperBound;
            slider.minValue = lowerBound;
            slider.maxValue = upperBound;
            OnValueChanged(lowerBound);
            RefreshMinMaxValueTexts();
        }

        public void ClearSlider() {
            Target.LowerBound = 0;
            Target.UpperBound = 0;
            slider.minValue = 0;
            slider.maxValue = 0;
            slider.value = 0;
            OnValueChanged(0);
            HideMinMaxValueTexts();
            slider.interactable = false;
        }
        
        public override UIResult Handle(UIEvent evt) {
            if (!slider.interactable) {
                return UIResult.Ignore;
            }
            
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValueAlt:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.IncreaseValueAlt:
                    Target.IncreaseValue();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValueAlt:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.DecreaseValueAlt:
                    Target.DecreaseValue();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
    }
}