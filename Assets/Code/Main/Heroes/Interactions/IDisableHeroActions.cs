using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Interactions {
    /// <summary>
    /// Marker interface for models that disables hero actions
    /// </summary>
    public interface IDisableHeroActions : IModel {
        bool HeroActionsDisabled(IHeroAction heroAction);
    }
}