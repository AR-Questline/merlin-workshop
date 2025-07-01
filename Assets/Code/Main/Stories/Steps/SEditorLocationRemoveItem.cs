using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Remove Item"), NodeSupportsOdin]
    public class SEditorLocationRemoveItem : EditorStep {
        public LocationReference location;
        public ItemSpawningData itemData;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationRemoveItem {
                location = location,
                itemData = itemData,
            };
        }
    }

    public partial class SLocationRemoveItem : StoryStepWithLocationRequirement {
        public LocationReference location;
        public ItemSpawningData itemData;

        protected override LocationReference RequiredLocations => location;
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(itemData);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationRemoveItem;

            [Saved] ItemSpawningData _itemData;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(ItemSpawningData itemData) {
                _itemData = itemData;
            }

            public override void Execute(Location location) {
                if (location.TryGetElement<SearchAction>(out var searchAction)) {
                    // Search action handles inventory as well
                    searchAction.RemoveItem(_itemData.ItemTemplate(null));
                } else if (location.TryGetElement<IInventory>(out var inventory)) {
                    if (SNpcRemoveItem.TryGetItem(inventory, _itemData, out Item item)) {
                        ItemUtils.RemoveItem(item, _itemData.quantity == 0 ? 1 : Mathf.Abs(_itemData.quantity));
                    }
                }
            }
        }
    }
}