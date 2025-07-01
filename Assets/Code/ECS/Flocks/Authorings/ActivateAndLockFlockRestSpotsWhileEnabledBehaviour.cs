using UnityEngine;

namespace Awaken.ECS.Flocks {
    public class ActivateAndLockFlockRestSpotsWhileEnabledBehaviour : MonoBehaviour {
        public void OnEnable() {
            FlockRestSpotSystem.ForceActivateAllRestSpots(float.PositiveInfinity);
        }

        public void OnDisable() {
            FlockRestSpotSystem.StopForceActiveAllRestSpots();
            
        }
    }
}