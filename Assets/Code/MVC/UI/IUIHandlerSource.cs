using System.Collections.Generic;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.MVC.UI
{
    /// <summary>
    /// Implementations of this interface are used to tell UISystem how
    /// to look for handlers interacting with mouse/keyboard events.
    /// </summary>
    public interface IUIHandlerSource : IElement<GameUI> {
        UIContext Context { get; }
        int Priority { get; }
        void ProvideHandlers(UIPosition position, List<IUIAware> handlers);
    }
}