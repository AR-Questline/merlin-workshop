using System;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    [Serializable]
    public class SpherePrimitiveProvider : MonoBehaviour, IAreaPrimitiveProvider {
        [SerializeField] float radius = 10;

        public IAreaPrimitive Spawn() => new SpherePrimitive(transform.position, radius);

        void OnDrawGizmosSelected() {
            using var gizmosColor = new GizmosColor(IAreaPrimitiveProvider.Color);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}