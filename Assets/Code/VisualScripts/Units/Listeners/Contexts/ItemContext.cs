using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    [UnityEngine.Scripting.Preserve]
    public class ItemContext : IListenerContext {
        public IModel Model => Item;
        public Location Location => null;
        public ICharacter Character => Item.Owner?.Character;
        public IAlive Alive => Item.Owner as IAlive ?? Item.Owner?.Character;
        public Skill Skill => null;
        public Item Item { get; }
        public Status Status => null;

        public ItemContext(Item item) {
            Item = item;
        }
    }
}