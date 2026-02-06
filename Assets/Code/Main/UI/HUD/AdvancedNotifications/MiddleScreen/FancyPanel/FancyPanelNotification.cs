using System;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    public partial class FancyPanelNotification : AdvancedNotification {
        public readonly string text;
        readonly Type _viewType;

        public FancyPanelNotification(string text, Type viewType) {
            this.text = text;
            this._viewType = viewType;
        }
        
        public override void Show() {
            World.SpawnView(this, _viewType, true);
        }
    }
}