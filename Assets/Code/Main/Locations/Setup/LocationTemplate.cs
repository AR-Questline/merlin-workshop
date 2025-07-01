using Awaken.TG.Assets;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Locations.Setup {
    public class LocationTemplate : Template {
        public override bool StartEnabled => false;
        
        /// <summary>
        /// Used to spawn runtime locations with default transform
        /// </summary>
        public Location SpawnLocation(ARAssetReference prefabReferenceOverride = null, string overridenLocationName = "", Scene? spawnScene = null) {
            RuntimeLocationData data = new(this, Vector3.zero, Quaternion.identity, Vector3.one, prefabReferenceOverride, overridenLocationName, spawnScene);
            return AddLocationToWorld(data);
        }
        
        /// <summary>
        /// Used to spawn runtime locations with all possible parameters, required minimum is initial position
        /// </summary>
        public Location SpawnLocation(Vector3 initialPosition, Quaternion? initialRotation = null, Vector3? initialScale = null, ARAssetReference prefabReferenceOverride = null, string overridenLocationName = "", Scene? spawnScene = null) {
            RuntimeLocationData data = new(this, initialPosition, initialRotation ?? Quaternion.identity, initialScale ?? Vector3.one, prefabReferenceOverride, overridenLocationName, spawnScene);
            return AddLocationToWorld(data);
        }
        
        Location AddLocationToWorld(in RuntimeLocationData data) {
            return World.Add(LocationCreator.CreateRuntimeLocation(this.name, data));
        }
    }
}