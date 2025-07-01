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
    [Element("Game/Shop: Open"), NodeSupportsOdin]
    public class SEditorOpenShop : EditorStep {
        [Tooltip("Should the NPC say something before opening the shop?")]
        public bool bark = true;
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenShop {
                bark = bark,
                locationRef = locationRef
            };
        }
    }

    public partial class SOpenShop : StoryStep {
        public bool bark = true;
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};
        
        public override StepResult Execute(Story story) {
            Location location = locationRef.FirstOrDefault(story);
            Shop shop = location?.TryGetElement<Shop>();
            if (shop == null || story.Hero == null) {
                Log.Important?.Error($"There is no shop attached to location {location} or no hero {story.Hero}");
                return StepResult.Immediate;
            }

            var result = new StepResult();
            OpenShop(location, shop, story, result);
            return result;
        }

        void OpenShop(Location location, Shop shop, Story api, StepResult result) {
            Bark();
            return;

            void Bark() {
                if (!bark || !location.TryGetElement(out BarkElement barks) || !barks.Bark(BarkBookmarks.OpenShop, BarkElement.BarkType.Important, false, out var barkStory)) {
                    OpenUI(api);
                    return;
                }

                barkStory.ListenTo(Model.Events.AfterDiscarded, () => OpenUI(api), api);
            }
            
            void OpenUI(Story storyApi) {
                storyApi.Clear();
                var shopUI = shop.OpenShop();
                shopUI.ListenTo(Model.Events.AfterDiscarded, OnShopClose, api);
            }
            
            void OnShopClose() {
                result.Complete();
            }
        }

        public override string GetKind(Story story) {
            return "Shop";
        }
    }
}