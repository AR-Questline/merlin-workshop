using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface IItemOwner : IModel {
        IInventory Inventory { get; }
        [CanBeNull] ICharacter Character { get; }
        [CanBeNull] IEquipTarget EquipTarget { get; }
        bool CanUseAdditionalHands => false;

        // === Events
        public static class Events {
            [UnityEngine.Scripting.Preserve] public static readonly Event<IItemOwner, Material> WeaponTrailChanged = new(nameof(WeaponTrailChanged));
            [UnityEngine.Scripting.Preserve] public static readonly Event<IItemOwner, Crime> ItemStolen = new(nameof(ItemStolen));
        }

        public static class Relations {
            public static readonly RelationPair<IItemOwner, Item> Ownership = new(typeof(Relations), Arity.One, nameof(Owns), Arity.Many, nameof(OwnedBy));
            /// <summary>
            /// All items owned by this owner (Regardless of state)
            /// </summary>
            public static readonly Relation<IItemOwner, Item> Owns = Ownership.LeftToRight;
            public static readonly Relation<Item, IItemOwner> OwnedBy = Ownership.RightToLeft;
        }
    }
}