using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// This element is used to spawn temporary item in hero inventory. It will be removed after duration and restore original item that was replaced.
    /// </summary>
    public partial class TemporaryItem : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.TemporaryItem;

        [Saved] WeakModelRef<Item> _originalItem;
        [Saved] WeakModelRef<LockItemSlot> _itemSlotLocker;
        [Saved] IDuration _duration;
        [Saved] Dictionary<HeroLoadout, EquipmentSlotType> _loadouts = new();
        [Saved] List<HeroLoadoutSlotLocker> _lockers = new();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public TemporaryItem() {}

        public TemporaryItem(Item originalItem, LockItemSlot itemSlotLocker, IDuration duration, Dictionary<HeroLoadout, EquipmentSlotType> loadouts,
            List<HeroLoadoutSlotLocker> lockers) {
            _originalItem = originalItem;
            _itemSlotLocker = itemSlotLocker;
            _duration = duration;
            _loadouts = loadouts;
            _lockers = lockers;
        }

        protected override void OnInitialize() {
            ParentModel.AddElement(new DiscardParentAfterDuration(_duration));
        }

        protected override void OnRestore() { }

        protected override void OnDiscard(bool fromDomainDrop) {
            _lockers.ForEach(l => l.Discard());
            Item originalItem = _originalItem.Get();
            foreach ((HeroLoadout loadout, EquipmentSlotType slot) in _loadouts) {
                loadout.EquipItem(slot, originalItem);
            }
            _itemSlotLocker.Get()?.Discard();
        }
    }
}