using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public interface IPortalOverride : IElement<Portal> {
        bool Override { get; }
        void Execute(Hero hero);
    }
}