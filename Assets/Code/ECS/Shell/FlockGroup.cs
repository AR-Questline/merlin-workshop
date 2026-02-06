using FMODUnity;
using UnityEngine;

namespace Awaken.ECS.Flocks.Authorings {
    public class FlockGroup : MonoBehaviour {
        public const int MaxEntitiesCount = 300;
        public EventReference groupFlyingEvent;
        public EventReference groupRestingEvent;
        public EventReference groupTakeOffEvent;
        public EventReference restingSoundEvent;
        public EventReference flyingSoundEvent;
        public EventReference takeOffEvent;
        public EventReference landEvent;
    }
}