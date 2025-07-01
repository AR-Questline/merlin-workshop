using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    public partial class InputItemQuantityUI : InputValueUI<int> {
        public sealed override bool IsNotSaved => true;

        int IncreaseStep { get; }
        public Item Item { get; private set; }
        public int UpperBound { get; set; }
        public int LowerBound { get; set; }
        public int QuantityMultiplayer { get; set; }
        public bool WithAlwaysPresentHandlers { get; private set; }

        public new static class Events {
            public static readonly Event<InputItemQuantityUI, int> SliderValueUpdated = new(nameof(SliderValueUpdated));
        }

        public InputItemQuantityUI(Item item = null, int upperBound = 1, int quantityMultiplayer = 1, bool withAlwaysPresentHandlers = true) {
            Item = item;
            UpperBound = Item?.Quantity ?? upperBound;
            Value = 1;
            LowerBound = 1;
            IncreaseStep = 1;
            QuantityMultiplayer = quantityMultiplayer;
            WithAlwaysPresentHandlers = withAlwaysPresentHandlers;
        }

        public void IncreaseValue() {
            ChangeValue(IncreaseStep);
        }

        public void DecreaseValue() {
            ChangeValue(-IncreaseStep);
        }

        public void ChangeSliderValue(float value) {
            Value = (int)value;
            this.Trigger(Events.SliderValueUpdated, Value);
        }

        public override void ChangeValue(int step) {
            var newValue = Mathf.Clamp(Value + step, LowerBound, UpperBound);
            base.ChangeValue(newValue);
        }
    }
}