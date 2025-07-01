using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Stories {
    public class StoryConfig {
        public Hero hero;
        public List<Location> locations = new();
        public Item item;
        public StoryBookmark bookmark;
        public Type viewType;
        public WeakModelRef<Story> parentStory;

        public static StoryConfig Base(StoryBookmark bookmark, Type viewType) => new StoryConfig(Hero.Current, null, bookmark, viewType);

        [UnityEngine.Scripting.Preserve]
        public static StoryConfig Item(Location location, Item item, StoryBookmark bookmark, Type viewType) =>
            Location(location, bookmark, viewType).WithItem(item);

        public static StoryConfig Location(Location location, StoryBookmark bookmark, Type viewType) =>
            Base(bookmark, viewType).WithLocation(location);

        public static StoryConfig Interactable(IInteractableWithHero interactable, StoryBookmark bookmark, Type viewType) =>
            interactable is Location location 
                ? Location(location, bookmark, viewType) 
                : Base(bookmark, viewType);

        public StoryConfig(Hero hero, Location location, StoryBookmark bookmark, Type viewType) {
            this.hero = hero;
            locations.Add(location);
            this.bookmark = bookmark;
            this.viewType = viewType;
        }

        public StoryConfig WithItem(Item item) {
            this.item = item;
            return this;
        }

        public StoryConfig WithLocation(Location location) {
            locations.Add(location);
            return this;
        }
        
        public StoryConfig WithLocations(IEnumerable<WeakModelRef<Location>> locationRefs) {
            this.locations.AddRange(locationRefs.Select(locRef => locRef.Get()).Where(l => l is { HasBeenDiscarded: false }));
            return this;
        }
    }
}