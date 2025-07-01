using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers {
    public class ListOfAwaresWithHandlers : HandlerContainer {
        List<AwareWithHandlers> _awares;

        public ListOfAwaresWithHandlers(string name, IEnumerable<IUIAware> awares, IEnumerable<ISmartHandler> smartHandlers) : base(name) {
            _awares = new List<AwareWithHandlers>(awares.Select(a => new AwareWithHandlers(a, smartHandlers)));
        }

        public override IEnumerable<IHandlerItem> Handlers => _awares;
    }
}