using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    public abstract partial class InputValueUI<TValue> : Element<IModel> where TValue : IConvertible {
        public TValue Value { get; protected set; }
        
        public new static class Events {
            public static readonly Event<InputValueUI<TValue>, TValue> ValueUpdated = new(nameof(ValueUpdated));
        }
        
        public virtual void ChangeValue(TValue newValue) {
            if (!Equals(newValue, Value)) {
                Value = newValue;
                this.Trigger(Events.ValueUpdated, Value);
            }
        }
    }
}