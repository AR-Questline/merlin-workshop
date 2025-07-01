using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public struct VFXAttachVC : IApplicableToVFX {
        public Location location;
        
        public VFXAttachVC(Location location) {
            this.location = location;
        }

        public void ApplyToVFX(VisualEffect vfx, GameObject gameObject) {
            ApplyToVFX(location, gameObject);
        }
        
        public static void ApplyToVFX(Location location, GameObject gameObject) {
            foreach (var vc in gameObject.GetComponentsInChildren<ViewComponent>(true)) {
                vc.Attach(World.Services, location, location.MainView);
            }
        }
    }
}
