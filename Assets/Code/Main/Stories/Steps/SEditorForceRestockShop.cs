using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Shop: Force Restock"), NodeSupportsOdin]
    public class SEditorForceRestockShop : EditorStep {
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SForceRestockShop {
                locationRef = locationRef
            };
        }
    }

    public partial class SForceRestockShop : StoryStep {
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};
        
        public override StepResult Execute(Story story) {
            Location location = locationRef.FirstOrDefault(story);
            Shop shop = location?.TryGetElement<Shop>();
            if (shop == null) {
                Log.Important?.Error($"There is no shop attached to location {location}");
                return StepResult.Immediate;
            }

            shop.Restock(true);
            return StepResult.Immediate;
        }
    }
}