using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Actions
{
    /// <summary>
    /// This interface is intended for elements of locations/places. An element implementing
    /// this interface can provide additional actions for a location.
    /// </summary>
    public interface ILocationActionProvider : IElement {
        IEnumerable<IHeroAction> GetAdditionalActions(Hero hero);
    }
}
