using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Views {
    public interface IVLocationWithState : IView<Location> {
        public void UpdateState();
    }
}