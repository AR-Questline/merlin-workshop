using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public interface IShortcut : IModel { }
    
    public static class ShortcutUtils {
        public static bool IsActive(this IShortcut shortcut) {
            return UIStateStack.Instance.State.ShortcutLayer.ContainsShortcut(shortcut);
        }
    }
}