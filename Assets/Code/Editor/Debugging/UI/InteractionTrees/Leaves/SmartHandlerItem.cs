using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Leaves {
    public class SmartHandlerItem : IHandlerItem {
        public ISmartHandler Handler { get; }

        public string Name => ItemName(Handler);
        public UIResult? Result { get; set; }

        public SmartHandlerItem(ISmartHandler handler) {
            Handler = handler;
        }
        
        public static string ItemName(ISmartHandler handler) {
            return handler.GetType().Name;
        }
    }
}