using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public interface IListenerContext {
        IModel Model { get; }
        Location Location { get; }
        ICharacter Character { get; }
        IAlive Alive { get; }
        Skill Skill { get; }
        Item Item { get; }
        Status Status { get; }
    }
}