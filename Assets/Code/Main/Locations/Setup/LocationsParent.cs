using UnityEngine;

namespace Awaken.TG.Main.Locations.Setup {
    /// <summary>
    /// Marker script for GameObject that is the parent of all locations in the scene
    /// </summary>
    public class LocationsParent : MonoBehaviour {
        void Awake() {
            Destroy(this);
        }
    }
}