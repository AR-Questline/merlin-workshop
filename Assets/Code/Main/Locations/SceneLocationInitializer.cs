using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    public partial class SceneLocationInitializer : LocationInitializer {
        public override ushort TypeForSerialization => SavedTypes.SceneLocationInitializer;

        public override void PrepareSpec(Location location) {
            Spec = World.Services.Get<SpecSpawner>().FindSpecFor(location);
        }
        
        public override Transform PrepareViewParent(Location location) {
            var viewParent = Spec.transform;

            // If location is suppose to move then don't dirty whole map hierarchy
            if (!location.IsNonMovable) {
                Spec.GetLocationId(); // Force cache TODO: probably not needed
                viewParent.SetParent(World.Services.Get<ViewHosting>().LocationsHost(location.CurrentDomain, Spec.gameObject.scene));
            }
            
            viewParent.GetPositionAndRotation(out var specInitialPosition, out var specInitialRotation);
            SpecInitialPosition = specInitialPosition;
            SpecInitialRotation = specInitialRotation;
            SpecInitialScale = viewParent.localScale;
            return viewParent;
        }
    }
}
