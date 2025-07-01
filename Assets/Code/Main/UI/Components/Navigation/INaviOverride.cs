using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.UI.Components.Navigation {
    public interface INaviOverride {
        UIResult Navigate(UINaviAction direction);
    }
}