using System.Collections.Generic;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees;
using Awaken.TG.Editor.Debugging.UI.UIEventTypes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.Editor.Debugging.UI {
    public class FrameData {
        Dictionary<UIEventType, InteractionTree> _handlersByEvent = new();

        public IEnumerable<UIEventType> Events => _handlersByEvent.Keys;
        public InteractionTree InteractionsOf(UIEventType eventType) => _handlersByEvent[eventType];
        public (string, UIResult?) HandlerAndResultOf(UIEventType eventType) => InteractionsOf(eventType).HandlerAndResult;

        public void AddEvent(UIEventType eventType, IEnumerable<ISmartHandler> handlers, IEnumerable<IUIAware> uiAwares) {
            _handlersByEvent.Add(eventType, new InteractionTree(handlers, uiAwares));
        }

        public void AddResultOfHandlerBeforeDelivery(UIEventType eventType, ISmartHandler handler, UIResult result) {
            _handlersByEvent[eventType].AddResultOfHandlerBeforeDelivery(handler, result);
        }

        public void AddResultOfHandling(UIEventType eventType, IUIAware aware, UIResult result) {
            _handlersByEvent[eventType].AddResultOfHandling(aware, result);
        }
        public void AddResultOfHandlerBeforeHandling(UIEventType eventType, IUIAware aware, ISmartHandler handler, UIResult result) {
            _handlersByEvent[eventType].AddResultOfHandlerBeforeHandling(aware, handler, result);
        }
        public void AddResultOfHandlerAfterHandling(UIEventType eventType, IUIAware aware, ISmartHandler handler, UIResult result) {
            _handlersByEvent[eventType].AddResultOfHandlerAfterHandling(aware, handler, result);
        }
    }
}