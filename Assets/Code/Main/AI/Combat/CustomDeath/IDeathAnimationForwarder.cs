using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public interface IDeathAnimationForwarder : IElement<Location> {
        public CustomDeathAnimation CustomDeathAnimation { get; }
    }
}