using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public class AxisAlignedBoxPrimitiveProvider : MonoBehaviour, IAreaPrimitiveProvider {
        [SerializeField] Vector3 size = new(10, 10, 10);
        
        public IAreaPrimitive Spawn() => new AxisAlignedBoxPrimitive(new Bounds(transform.position, size));
        
        void OnDrawGizmosSelected() {
            using var gizmosColor = new GizmosColor(IAreaPrimitiveProvider.Color);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}