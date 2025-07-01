using System.Collections.Generic;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers {
    public class ListOfHandlers : HandlerContainer {
        List<IHandlerItem> _items;

        public ListOfHandlers(string name, IEnumerable<IHandlerItem> items) : base(name) {
            _items = new List<IHandlerItem>(items);
        }

        public override IEnumerable<IHandlerItem> Handlers => _items;
    }
}