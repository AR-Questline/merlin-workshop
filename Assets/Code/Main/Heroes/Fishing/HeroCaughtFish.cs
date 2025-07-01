using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Heroes.Fishing {
    public partial class HeroCaughtFish : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroCaughtFish;

        [Saved] public List<FishEntry> caughtFish = new();

        public new static class Events {
            public static readonly Event<Hero, FishEntry> CaughtNew = new(nameof(CaughtNew));
        }

        public void AddToCaughtFishCollection(FishEntry fish) {
            bool isNew = caughtFish.All(f => f.Id != fish.Id);
            if (isNew) {
                caughtFish.Add(fish);
                ParentModel.Trigger(Events.CaughtNew, fish);
            }
        }
        
        public bool WasPreviouslyCaught(string guid, out FishEntry fish) {
            foreach (var fishEntry in caughtFish) {
                if (fishEntry.Id == guid) {
                    fish = fishEntry;
                    return true;
                }
            }
            
            fish = default;
            return false;
        }

        public void UpdateRecord(FishEntry fish) {
            caughtFish.RemoveAll(f => f.Id == fish.Id);
            caughtFish.Add(fish);
        }
    }
}
