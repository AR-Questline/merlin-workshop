using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredDistanceCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredDistanceCondition;

        const float MinRequiredDistanceSqr = 70f * 70f;
        const float MaxRequiredDistanceSqr = 150f * 150f;

        [Saved] WeakModelRef<Location> _locationRef;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredDistanceCondition() {}
        
        public DeferredDistanceCondition(Location locationRef) {
            _locationRef = locationRef;
        }

        public override bool Fulfilled() {
            if (World.Only<DeferredSystem>().OverrideDistanceConditions) {
                return true;
            }

            if (!_locationRef.TryGet(out Location location)) {
                return true;
            }

            Hero hero = Hero.Current;
            float sqrDistance = location.Coords.SquaredDistanceTo(hero.Coords);
            if (sqrDistance <= MinRequiredDistanceSqr) {
                // False if closer than MinRequiredDistanceSqr meters
                return false;
            }

            float dotProduct = Vector3.Dot(hero.Forward(), (location.Coords - hero.Coords).normalized);
            // Return true if Hero is not looking at the target or is further than MaxRequiredDistanceSqr meters
            return dotProduct < 0 || sqrDistance >= MaxRequiredDistanceSqr;
        }
    }
}