using System;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    public partial class LowerInfoNotification : LowerFancyPanelNotification {
        VLowerInfoNotification View => View<VLowerInfoNotification>();
        
        public LowerInfoNotification(string text, Type viewType) : base(text, viewType) { }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, _ => Discard());
        }

        public void OverrideText(string newText) {
            View.SetText(newText);
            View.ExtendAnimation();
        }
    }
}