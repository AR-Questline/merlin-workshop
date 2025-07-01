using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility;
using UnityEngine;

namespace Awaken.TG
{
    public class SpecSpawnTester : MonoBehaviour {
        void Awake() {
            foreach (LocationTemplate template in GetComponentsInChildren<LocationTemplate>()) {
                Location location = template.SpawnLocation();
            }
        }
    }
}
