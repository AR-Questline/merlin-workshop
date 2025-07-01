using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Move Item to Another Location"), NodeSupportsOdin]
    public class SEditorMoveItemBetweenLocations : EditorStep {
        public ItemSpawningData itemData;
        public bool takeAllCopies;
        
        [Header("MoveFrom")]
        public LocationReference moveFromLocationRef;

        [Header("MoveTo")]
        public bool toHeroEq;
        [HideIf(nameof(toHeroEq))] public LocationReference moveToLocationRef;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SMoveItemBetweenLocations {
                itemData = itemData,
                takeAllCopies = takeAllCopies,
                moveFromLocationRef = moveFromLocationRef,
                toHeroEq = toHeroEq,
                moveToLocationRef = moveToLocationRef
            };
        }
    }
    
    public partial class SMoveItemBetweenLocations : StoryStep {
        public ItemSpawningData itemData;
        public bool takeAllCopies;
        
        public LocationReference moveFromLocationRef;

        public bool toHeroEq;
        public LocationReference moveToLocationRef;
        
        public override StepResult Execute(Story story) {
            var moveFromLocation = moveFromLocationRef.MatchingLocations(story).FirstOrDefault();
            var moveToInventory = toHeroEq ? story.Hero.Inventory : moveToLocationRef.MatchingLocations(story).FirstOrDefault()?.TryGetElement<IInventory>();
            if (moveFromLocation == null || moveToInventory == null) {
                Log.Minor?.Error($"SMoveItemBetweenLocations can't find valid locations {story}");
                return StepResult.Immediate;
            }
            MoveItems(moveFromLocation, moveToInventory);
            return StepResult.Immediate;
        }
        
        void MoveItems (Location moveFromLocation, IInventory moveToInventory) {
            if (moveFromLocation.TryGetElement<SearchAction>(out var searchAction)) {
                // Search action handles inventory as well
                if (takeAllCopies) {
                    searchAction.MoveItem(moveToInventory, itemData.ItemTemplate(null));
                } else {
                    searchAction.MoveItem(moveToInventory, itemData.ItemTemplate(null), itemData.quantity);
                }
            } else if (moveFromLocation.TryGetElement<IInventory>(out var inventory)) {
                var template = itemData.ItemTemplate(null);
                IEnumerable<Item> items;
                if (takeAllCopies) {
                    items = inventory.GetSimilarItemsInInventory(i => i.Template == template);
                } else {
                    items = inventory.GetSimilarItemsInInventory(i => i.Template == template, itemData.quantity);
                }
                foreach (var item in items) {
                    item.MoveTo(moveToInventory);
                }
            }
        }
    }
}