using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Views;
using Awaken.Utility.GameObjects;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class NpcDummyTeleporterToNavmesh : MonoBehaviour {
        [SerializeField] bool onlyIfHaveImportantItems;
        
        void OnTriggerEnter(Collider other) {
            if (other.TryGetComponentInParent(out VLocation vLocation) && vLocation.Target.TryGetElement<NpcDummy>(out var dummy)) {
                if (onlyIfHaveImportantItems && dummy.DoesntHaveImportantItems) {
                    return;
                }
                TeleportToClosestNavMesh(vLocation.transform, vLocation.Target, dummy);
            }
        }

        static void TeleportToClosestNavMesh(Transform viewTransform, Location location, NpcDummy dummy) {
            NNInfo nearest = AstarPath.active.GetNearest(location.Coords, NNConstraint.Walkable);
            foreach (var rb in viewTransform.GetComponentsInChildren<Rigidbody>()) {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            dummy.HipsSocket.localPosition = Vector3.zero;
            location.SafelyMoveTo(nearest.position);
        }
    }
}