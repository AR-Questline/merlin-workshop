using System.Collections.Generic;

namespace Awaken.TG.MVC.UI {
    public interface IUIAwareContainer : IUIAware {
        IReadOnlyList<IUIAware> UIAwares { get; }
    }
}