using System;
using System.Collections.Generic;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;

namespace Awaken.TG.MVC.UI.Universal {
    /// <summary>
    /// Model that filters input, allows you to limit input to few UI elements.
    /// </summary>
    [SpawnsView(typeof(VSelectiveInputBlocker))]
    public partial class SelectiveInputBlocker : Element<Model>, IUIAware {
        public sealed override bool IsNotSaved => true;

        readonly HashSet<IUIAware> _whiteList = new HashSet<IUIAware>();
        readonly HashSet<string> _actionWhiteList = new HashSet<string>();
        AlwaysPresentHandlers _alwaysPresentHandler;

        public InputBlockerConfig Config { get; }

        public SelectiveInputBlocker(InputBlockerConfig inputBlockerConfig) {
            Config = inputBlockerConfig;
            if (inputBlockerConfig.uiAware != null) {
                _whiteList = new HashSet<IUIAware>(inputBlockerConfig.uiAware);
            }

            if (inputBlockerConfig.actionNames != null) {
                _actionWhiteList = new HashSet<string>(inputBlockerConfig.actionNames);
            }
        }

        protected override void OnInitialize() {
            _alwaysPresentHandler = new AlwaysPresentHandlers(UIContext.Keyboard, this);
            World.Only<GameUI>().AddElement(_alwaysPresentHandler);
        }

        public bool IsAllowed(IUIAware target) {
            if (target == null) return false;

            if (_whiteList != null) {
                return _whiteList.Contains(target);
            }

            return false;
        }

        // keyboard handle
        public UIResult Handle(UIEvent evt) {
            if (evt is UIAction action) {
                if (action.Name == KeyBindings.UI.Generic.Cancel) {
                    return UIResult.Ignore;
                }

                bool whitelistedKeyDown = action.IsButtonDown && _actionWhiteList.Contains(action.Name);
                bool whitelistedKeyHeld = action.IsButtonHeld && _actionWhiteList.Contains(action.Name);
                bool whitelistedAxisUsed = action is UIAxisAction && action.IsOutsideOfDeadZone;
                if (whitelistedKeyDown || whitelistedKeyHeld || whitelistedAxisUsed) {
                    if (Config.discardOnInputAccepted) {
                        Discard();
                    }

                    return UIResult.Ignore;
                }
            }

            return UIResult.Prevent;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _alwaysPresentHandler.Discard();
        }
    }

    [Serializable]
    public struct MouseClickEvent {
        public bool use;
        public bool consumeEvent;
        public int[] buttons;

        public MouseClickEvent(bool use = false, bool consumeEvent = false, int[] buttons = null) {
            this.use = use;
            this.consumeEvent = consumeEvent;
            this.buttons = buttons;
        }
    }

    public class InputBlockerConfig {
        public bool discardOnInputAccepted;
        public MouseClickEvent mouseEvent;
        public IEnumerable<string> actionNames;
        public IEnumerable<IUIAware> uiAware;
        public bool allowHovering;

        public InputBlockerConfig(IEnumerable<IUIAware> uiAware = null, IEnumerable<string> actionNames = null, MouseClickEvent? mouseEvent = null,
            bool discardOnInputAccepted = false) {
            this.uiAware = uiAware;
            this.actionNames = actionNames;
            this.mouseEvent = mouseEvent ?? new MouseClickEvent();
            this.discardOnInputAccepted = discardOnInputAccepted;
        }
    }
}