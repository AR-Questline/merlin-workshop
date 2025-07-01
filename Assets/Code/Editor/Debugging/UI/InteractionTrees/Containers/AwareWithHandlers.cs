using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Leaves;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers {
    public class AwareWithHandlers : HandlerContainer {
        public ListOfHandlers handlersBefore;
        public ListOfHandlers handlersAfter;
        
        UIAwareItem _awareItem;
        ListOfAwaresWithHandlers _containerContent;

        public AwareWithHandlers(IUIAware aware, IEnumerable<ISmartHandler> smartHandlers) : base(UIAwareItem.ItemName(aware)) {
            handlersBefore = new ListOfHandlers("Handlers Before", smartHandlers.Select(h => new SmartHandlerItem(h)));
            handlersAfter = new ListOfHandlers("Handlers After", smartHandlers.Select(h => new SmartHandlerItem(h)));
            _awareItem = new UIAwareItem(aware, this);
            if (aware is IUIAwareContainer container) {
                _containerContent = new ListOfAwaresWithHandlers("Container Content", container.UIAwares, smartHandlers);
            }
        }

        public override IEnumerable<IHandlerItem> Handlers {
            get {
                yield return handlersBefore;
                yield return _awareItem;
                if (_containerContent != null) yield return _containerContent;
                yield return handlersAfter;
            }
        }
    }
}