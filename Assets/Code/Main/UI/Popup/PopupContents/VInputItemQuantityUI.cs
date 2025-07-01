using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    [UsesPrefab("Story/PopupContents/" + nameof(VInputItemQuantityUI))]
    public class VInputItemQuantityUI : View<InputItemQuantityUI>, IFocusSource, IUIAware {
        protected const float FadeDuration = 1f;
        const float ButtonHoldDuration = 0.4f;
        
        [SerializeField] protected TextMeshProUGUI quantityText;
        [SerializeField] protected QuantitySlider slider;
        [SerializeField] protected ARButton buttonIncrease;
        [SerializeField] protected ARButton buttonDecrease;
        [SerializeField] TextMeshProUGUI minValueText;
        [SerializeField] TextMeshProUGUI maxValueText;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;

        protected override void OnInitialize() {
            if (Target.WithAlwaysPresentHandlers) {
                World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
            }
            
            Target.ListenTo(InputValueUI<int>.Events.ValueUpdated, OnValueChanged, this);
            Target.ListenTo(InputItemQuantityUI.Events.SliderValueUpdated, OnValueChanged, this);
            
            slider.wholeNumbers = true;
            slider.minValue = Target.LowerBound;
            slider.maxValue = Target.UpperBound;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            OnValueChanged(Target.UpperBound);
            
            quantityText.SetText((Target.UpperBound * Target.QuantityMultiplayer).ToString());
            RefreshMinMaxValueTexts();

            ColorComponents();

            buttonIncrease.OnHold += hold => {
                if (hold > ButtonHoldDuration) {
                    Target.IncreaseValue();
                }
            };
            buttonDecrease.OnHold += hold => {
                if (hold > ButtonHoldDuration) {
                    Target.DecreaseValue();
                }
            };
            buttonIncrease.OnRelease += Target.IncreaseValue;
            buttonDecrease.OnRelease += Target.DecreaseValue;
        }

        protected void RefreshMinMaxValueTexts() {
            minValueText.SetText((Target.LowerBound * Target.QuantityMultiplayer).ToString());
            maxValueText.SetText((Target.UpperBound * Target.QuantityMultiplayer).ToString());
        }
        
        protected void HideMinMaxValueTexts() {
            minValueText.SetText("0");
            maxValueText.SetText("0");
        }

        protected virtual void OnValueChanged(int newValue) {
            if (newValue < Target.LowerBound || newValue > Target.UpperBound) {
                return;
            }

            slider.value = newValue;
            quantityText.SetText((newValue * Target.QuantityMultiplayer).ToString());
        }

        void OnSliderValueChanged(float newValue) {
            int value = (int) newValue;
            if (value < Target.LowerBound || value > Target.UpperBound) {
                return;
            }
            
            Target.ChangeSliderValue(value);
            OnValueChanged(value);
        }
        
        protected virtual void ColorComponents() { }
        

        public virtual UIResult Handle(UIEvent evt) {
            if (Target == null || Target.HasBeenDiscarded) {
                return UIResult.Ignore;
            }
            
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValue:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.IncreaseValue:
                    Target.IncreaseValue();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValue:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.DecreaseValue:
                    Target.DecreaseValue();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
    }
}