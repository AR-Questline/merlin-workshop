using System.Collections.Generic;

namespace Awaken.TG.MVC.UI.Handlers.Drags {
    public interface IDragNDropView : IDraggableView {
        IEnumerable<IUIAware> DragTargets();
    }
}