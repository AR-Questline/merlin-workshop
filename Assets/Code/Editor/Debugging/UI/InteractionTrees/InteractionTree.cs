using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Leaves;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees {
    public class InteractionTree : HandlerContainer {
        public ListOfHandlers beforeDelivery;
        public ListOfAwaresWithHandlers awaresWithHandlers;

        public InteractionTree(IEnumerable<ISmartHandler> handlers, IEnumerable<IUIAware> uiAwares) : base("Root") {
            beforeDelivery = new ListOfHandlers("Before Delivery", handlers.Select(h => new SmartHandlerItem(h)));
            awaresWithHandlers = new ListOfAwaresWithHandlers("UI Awares", uiAwares, handlers);
        }

        public void AddResultOfHandlerBeforeDelivery(ISmartHandler handler, UIResult result) {
            beforeDelivery.FindItemFor(handler).Result = result;
        }

        public void AddResultOfHandling(IUIAware aware, UIResult result) {
            awaresWithHandlers.FindItemFor(aware).Result = result;
        }
        public void AddResultOfHandlerBeforeHandling(IUIAware aware, ISmartHandler handler, UIResult result) {
            awaresWithHandlers.FindItemFor(aware).Parent.handlersBefore.FindItemFor(handler).Result = result;
        }
        public void AddResultOfHandlerAfterHandling(IUIAware aware, ISmartHandler handler, UIResult result) {
            awaresWithHandlers.FindItemFor(aware).Parent.handlersAfter.FindItemFor(handler).Result = result;
        }

        public override IEnumerable<IHandlerItem> Handlers {
            get {
                yield return beforeDelivery;
                yield return awaresWithHandlers;
            }
        }
    }
}