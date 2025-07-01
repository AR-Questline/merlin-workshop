using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.Housing.UnlockHouse;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/House: Unlock UI"), NodeSupportsOdin]
    public class SEditorOpenHouseUnlock : EditorStep {
        public ItemSpawningData homeKeyItem;
        [ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.UI)]
        public ShareableSpriteReference houseSprite;
        [LocStringCategory(Category.Housing)]
        public LocString houseName;
        [LocStringCategory(Category.Housing)]
        public LocString houseDescription;
        public int price;
        public LocationReference portalLocation;
        public bool disappearAfterUnlock = true;
        public StoryBookmark storyOnUnlock;
        
        Location _housingPortalLocation;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenHouseUnlock {
                homeKeyItem = homeKeyItem,
                houseSprite = houseSprite,
                houseName = houseName,
                houseDescription = houseDescription,
                price = price,
                portalLocation = portalLocation,
                disappearAfterUnlock = disappearAfterUnlock,
                storyOnUnlock = storyOnUnlock
            };
        }
    }

    public partial class SOpenHouseUnlock : StoryStep {
        public ItemSpawningData homeKeyItem;
        public ShareableSpriteReference houseSprite;
        public LocString houseName;
        public LocString houseDescription;
        public int price;
        public LocationReference portalLocation;
        public bool disappearAfterUnlock = true;
        public StoryBookmark storyOnUnlock;
        
        IEnumerable<Location> _housingPortalLocations;
        
        public override StepResult Execute(Story story) {
            _housingPortalLocations = portalLocation.MatchingLocations(story);
            if (!_housingPortalLocations.Any()) {
                Log.Important?.Error("Couldn't find matching portal location!");
                return StepResult.Immediate;
            }
            var locToDisappear = disappearAfterUnlock ? story.FocusedLocation : null;
            UnlockHousingUI.OpenUnlockHouseUI(new UnlockHousingData(houseName, houseDescription, price, houseSprite), () => OnUnlocked(locToDisappear, story));
            return StepResult.Immediate;
        }

        void OnUnlocked(Location locToDisappear, Story api) {
            if (locToDisappear != null) {
                var item = World.Add(new Item(homeKeyItem.ItemTemplate(api)));
                Hero.Current.HeroItems.Add(item);

                foreach (Location housingPortalLocation in _housingPortalLocations) {
                    housingPortalLocation.SetInteractability(LocationInteractability.Active);
                }
                
                locToDisappear.SetInteractability(LocationInteractability.Hidden);

                if (storyOnUnlock is { IsValid: true }) {
                    Story.StartStory(StoryConfig.Base(storyOnUnlock, null));
                }
            }
        }
    }
}