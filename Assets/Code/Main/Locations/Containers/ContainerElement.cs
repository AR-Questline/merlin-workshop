using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Containers {
    public partial class ContainerElement : Element<IContainerElementParent> {
        public sealed override bool IsNotSaved => true;

        public Item Item { get; }
        public ItemTemplate ItemTemplate => Item.Template;
        public bool Selected { get; private set; }
        
        public Crime Crime { get; }

        public ContainerElement(Item item, Location owner) {
            Item = item;
            Crime = owner.TryGetElement<NpcElement>(out var npc) && npc is { HasBeenDiscarded: false, IsAlive: true }
                        ? Crime.Pickpocket(item, npc.CrimeValue, owner)
                        : Crime.Theft(Item, owner);
        }

        public void Select() {
            Selected = true;
        }

        public void Deselect() {
            Selected = false;
        }
    }
}