using System;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.UI.UIEventTypes {
    public class UIEventType {
        Type Type { get; }
        
        public string NameColumn => Type.Name;
        public virtual string DataColumn => null;

        protected UIEventType(UIEvent evt) {
            Type = evt.GetType();
        }

        protected virtual bool Equals(UIEventType other) {
            return Type == other.Type;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is UIEventType eventType && Equals(eventType);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Type?.GetHashCode() ?? 0) * 397) ^ (DataColumn?.GetHashCode() ?? 0);
            }
        }

        public static UIEventType CreateFor(UIEvent evt) {
            return evt switch {
                UIAction action => new UIActionType(action),
                UIMouseButtonEvent mouse => new UIMouseButtonEventType(mouse),
                UIKeyEvent key => new UIKeyEventType(key),
                _ => new UIEventType(evt)
            };
        }
    }
    
    public class UIActionType : UIEventType {
        string ActionName { get; }
        
        public override string DataColumn => ActionName;

        public UIActionType(UIAction evt) : base(evt) {
            ActionName = evt.Data.actionName;
        }

        protected override bool Equals(UIEventType other) {
            return other is UIActionType otherAction 
                   && ActionName == otherAction.ActionName 
                   && base.Equals(other);
        }
    }

    public class UIMouseButtonEventType : UIEventType {
        int Button { get; }
        
        public override string DataColumn => Button.ToString();

        public UIMouseButtonEventType(UIMouseButtonEvent evt) : base(evt) {
            Button = evt.Button;
        }

        protected override bool Equals(UIEventType other) {
            return other is UIMouseButtonEventType mouseType
                && Button == mouseType.Button
                && base.Equals(other);
        }
    }
    
    public class UIKeyEventType : UIEventType {
        KeyCode KeyCode { get; }
        
        public override string DataColumn => KeyCode.ToString();

        public UIKeyEventType(UIKeyEvent evt) : base(evt) {
            KeyCode = evt.Key;
        }

        protected override bool Equals(UIEventType other) {
            return other is UIKeyEventType key 
                && KeyCode == key.KeyCode
                && base.Equals(other);
        }
    }
}