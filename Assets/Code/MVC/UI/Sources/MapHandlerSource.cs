using System.Collections.Generic;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.MVC.UI.Sources {
    /// <summary>
    /// Returns handlers only when map is interactive
    /// </summary>
    public partial class MapHandlerSource : AlwaysPresentHandlers {
        public MapHandlerSource(UIContext context, IUIAware handler, IModel owner, int priority = 0) : base(context, handler, owner, priority) { }
        [UnityEngine.Scripting.Preserve] public MapHandlerSource(UIContext context, IEnumerable<IUIAware> handlers, IModel owner, int priority = 0) : base(context, handlers, owner, priority) { }

        public override void ProvideHandlers(UIPosition _, List<IUIAware> handlers) {
            if (UIStateStack.Instance.State.IsMapInteractive) {
                base.ProvideHandlers(_, handlers);
            }
        }
    }
}