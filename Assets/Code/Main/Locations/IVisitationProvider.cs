using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations {
    public interface IVisitationProvider : IElement {
        bool IsVisited { get; }
    }
}