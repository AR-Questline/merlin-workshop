using System;
using System.Collections.Generic;
using System.Diagnostics;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.MVC.UI {
    public class GameUIProfiler {
        public event Action<UIEvent, IEnumerable<ISmartHandler>, IEnumerable<IUIAware>> NewEvent;
        public event Action<UIEvent, ISmartHandler, UIResult> BeforeDelivery;
        public event Action<UIEvent, IUIAware, UIResult> Handling;
        public event Action<UIEvent, IUIAware, ISmartHandler, UIResult> BeforeHandling;
        public event Action<UIEvent, IUIAware, ISmartHandler, UIResult> AfterHandling;

        [Conditional("DEBUG")]
        public void OnNewEvent(UIEvent evt, IEnumerable<ISmartHandler> smartHandlers, IEnumerable<IUIAware> awares) {
            NewEvent?.Invoke(evt, smartHandlers, awares);
        }

        [Conditional("DEBUG")]
        public void OnBeforeDelivery(UIEvent evt, ISmartHandler smartHandler, UIResult result) {
            BeforeDelivery?.Invoke(evt, smartHandler, result);
        }

        [Conditional("DEBUG")]
        public void OnHandling(UIEvent evt, IUIAware aware, UIResult result) {
            Handling?.Invoke(evt, aware, result);
        }

        [Conditional("DEBUG")]
        public void OnBeforeHandling(UIEvent evt, IUIAware aware, ISmartHandler smartHandler, UIResult result) {
            BeforeHandling?.Invoke(evt, aware, smartHandler, result);
        }

        [Conditional("DEBUG")]
        public void OnAfterHandling(UIEvent evt, IUIAware aware, ISmartHandler smartHandler, UIResult result) {
            AfterHandling?.Invoke(evt, aware, smartHandler, result);
        }
    }
}