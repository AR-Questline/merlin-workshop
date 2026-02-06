using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public abstract partial class AdvancedNotification : Element {
        public sealed override bool IsNotSaved => true;
        
        public virtual bool IsValid => true;
        public virtual bool IsMergeable => false;
        
        public virtual void Show() {} //TODO: remove this method when we rewrite advanced notifications to UIToolkit
    }
}