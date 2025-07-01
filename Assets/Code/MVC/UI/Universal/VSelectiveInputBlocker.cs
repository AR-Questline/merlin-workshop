using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Universal {
    [UsesPrefab("UI/VSelectiveInputBlocker")]
    [DefaultExecutionOrder(int.MaxValue)] // execute last
    public class VSelectiveInputBlocker : View<SelectiveInputBlocker>, IUIAware {

        float _initTime;
        
        public override Transform DetermineHost() => World.Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            _initTime = Time.realtimeSinceStartup;
            
            ListenForAddAndDiscard(typeof(MenuUI));
            ListenForAddAndDiscard(typeof(AllSettingsUI));
            ListenForAddAndDiscard(typeof(UserBugReporting));
        }

        void ListenForAddAndDiscard(Type t) {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded(t), this, RefreshVisibility);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded(t), this, RefreshVisibility);
        }

        public UIResult Handle(UIEvent evt) {
            // give some time, so user doesn't close tutorial too early
            if (Time.realtimeSinceStartup - _initTime < 1.25f) {
                return UIResult.Prevent;
            }
            
            // mouse click overlay
            if (Target.Config.mouseEvent.use && evt is UIEMouseDown md && Target.Config.mouseEvent.buttons.Contains(md.Button)) {
                if (Target.Config.discardOnInputAccepted) {
                    Target.Discard();
                }
                return Target.Config.mouseEvent.consumeEvent ? UIResult.Accept : UIResult.Ignore;
            }

            if (evt is UIEPointTo && Target.Config.allowHovering) {
                return UIResult.Ignore;
            }

            // create stack of allowed Handlers
            List<IUIAware> allowedStack = evt.ItemsBelow(this).Where(Target.IsAllowed).ToList();
            if (allowedStack.Any()) {
                // send event only to allowed Handlers
                World.Only<GameUI>().DeliverEvent(evt, null, allowedStack);
                if (evt is UIEMouseDown && Target.Config.discardOnInputAccepted) {
                    Target.Discard();
                }
                return UIResult.Accept;
            } else {
                return UIResult.Prevent;
            }
        }

        void RefreshVisibility() {
            bool shouldBeActive = !World.HasAny<MenuUI>() && !World.HasAny<AllSettingsUI>();
            gameObject.SetActive(shouldBeActive);
        }

        void LateUpdate() {
            transform.SetAsLastSibling();
        }
    }
}