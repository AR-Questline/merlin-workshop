using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.Main.UI.ButtonSystem {

    public interface IButtonTap : IModel {
        internal void OnTap();
        
        internal void Invoke();
    }

    public interface IButtonHold : IModel {
        float HoldTime => ButtonsHandler.HoldTime;
        internal void OnKeyDown();
        internal void OnKeyHeld(float percent);
        internal void OnKeyUp(bool completed = false);
        
        internal void Invoke();
    }
    
    public interface IButton : IButtonTap, IButtonHold {
        PressType ButtonPressType { get; }
        bool Accept(UIKeyAction action);
        void OnHoldInterrupted();
        bool ActionMatches(UIKeyAction action);

        new void Invoke();
        void IButtonHold.Invoke() => Invoke();
        void IButtonTap.Invoke() => Invoke();

        bool Tap => ButtonPressType == PressType.Tap;
        bool Hold => ButtonPressType == PressType.Hold;
        
        public enum PressType {
            Tap,
            Hold
        }
    }
}