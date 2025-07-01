using UnityEngine;

namespace Awaken.ECS.Flocks {
    public class ScareFlocksOnEnableBehaviour : MonoBehaviour {
        public void OnEnable() {
            FlockRestSpotSystem.ReleaseAllEntitiesInRestSpots();
        }
    }
}