using UnityEngine;

namespace Awaken.ECS.Authoring.LinkedEntities {
    public class SharedLinkedEntitiesLifetime : MonoBehaviour {
        public LinkedEntitiesAccess LinkedEntitiesAccess { get; private set; }
        public LinkedEntityLifetime LinkedEntityLifetime { get; private set; }

        void Awake() {
            LinkedEntitiesAccess = LinkedEntitiesAccess.GetOrCreate(gameObject);
            LinkedEntityLifetime = LinkedEntityLifetime.GetOrCreate(gameObject);
        }
    }
}
