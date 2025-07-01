using Awaken.TG.MVC.UI;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees {
    public interface IHandlerItem {
        public string Name { get; }
        public UIResult? Result { get; }
    }
}