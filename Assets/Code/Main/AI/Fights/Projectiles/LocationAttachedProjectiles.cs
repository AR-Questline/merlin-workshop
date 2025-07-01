using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public partial class LocationAttachedProjectiles : Element<Location> {
        public sealed override bool IsNotSaved => true;

        List<ProjectileLocationData> _projectileLocations = new();
        bool _releasing;
        
        LocationAttachedProjectiles() { }
        
        public static LocationAttachedProjectiles GetOrCreate(Location location) {
            return location.TryGetElement<LocationAttachedProjectiles>() ?? location.AddElement(new LocationAttachedProjectiles());
        }

        public void AddProjectileLocation(Arrow owner, Location physicalLocation, ItemSpawningDataRuntime spawningData) {
            IInventory inventory = ParentModel.Inventory;
            Item pickItem = null;
            if (inventory != null) {
                pickItem = new(spawningData);
                pickItem.AddElement<NoItemCrimeOverride>();
                
                pickItem = pickItem.MoveTo(inventory);
                
                if (pickItem == null) {
                    // Item was discarded upon adding to inventory. some other system took care of it
                    return;
                }

                if (!spawningData.ItemTemplate.CanStack || !_projectileLocations.Any(d => Equals(d.template, spawningData.ItemTemplate))) {
                    pickItem.ListenTo(Item.Events.QuantityChanged, OnQuantityChanged, physicalLocation);
                    pickItem.ListenTo(Events.BeforeDiscarded, OnBeforeDiscarded, physicalLocation);
                    pickItem.ListenTo(ICharacterInventory.Relations.ContainedBy.Events.AfterDetached, OnRelationDetached, physicalLocation);

                }
            }
            
            ProjectileLocationData projectileLocationData = new(owner, physicalLocation, spawningData.ItemTemplate, pickItem);
            _projectileLocations.Add(projectileLocationData);
        }

        /// <summary>
        /// Should only be called from Arrow.ReleaseSelf
        /// </summary>
        public void Release(Arrow owner, bool removeFromInventory = true) {
            if (_releasing) return;
            ProjectileLocationData data = _projectileLocations.FirstOrDefault(p => p.owner == owner);
            if (data == null) return;
            
            _releasing = true;
            data.physicalLocation?.Discard();
            if (removeFromInventory) {
                data.item?.DecrementQuantity();
            }
            _projectileLocations.Remove(data);
            
            _releasing = false;
            if (_projectileLocations.Count == 0) Discard();
        }

        void OnQuantityChanged(QuantityChangedData data) {
            if (_releasing || data.amount > 0) return;
            
            foreach (ProjectileLocationData locationData in _projectileLocations.Where(p => p.item == data.target).Take(-data.amount).ToArray()) {
                locationData.owner.ReleaseSelf(false);
            }
        }
        
        void OnBeforeDiscarded(Model item) {
            if (_releasing) return;
            
            foreach (ProjectileLocationData locationData in _projectileLocations.Where(pl => pl.item == item).ToArray()) {
                locationData.owner.ReleaseSelf(false);
            }
        }

        void OnRelationDetached(RelationEventData data) {
            if (_releasing) return;
            
            foreach (ProjectileLocationData locationData in _projectileLocations.Where(pl => pl.item == data.from).ToArray()) {
                locationData.owner.ReleaseSelf(false);
            }
        }
        
        class ProjectileLocationData {
            public readonly Arrow owner;
            public readonly Location physicalLocation;
            public readonly ItemTemplate template;
            public readonly Item item;

            public ProjectileLocationData(Arrow owner, Location physicalLocation, ItemTemplate template, Item item) {
                this.owner = owner;
                this.physicalLocation = physicalLocation;
                this.template = template;
                this.item = item;
            }
        }
    }
}