using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    public partial class FancyPanelNotification : Element<MiddleScreenNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly string text;
        readonly Type _viewType;

        public FancyPanelNotification(string text, Type viewType) {
            this.text = text;
            this._viewType = viewType;
        }
        
        public void Show() {
            World.SpawnView(this, _viewType, true);
        }
    }
}