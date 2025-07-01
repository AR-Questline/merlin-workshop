using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Paths {
    public class VertexPathSpec : MonoBehaviour {
        
        public List<Vector3> waypoints = new();

#if UNITY_EDITOR
        
        public bool snapToGround = true;
        
        [FoldoutGroup("Gizmos")] public bool draw;
        [FoldoutGroup("Gizmos")] public Color edgeColor = Color.white;
        [FoldoutGroup("Gizmos")] public Color vertexColor = Color.red;
        [FoldoutGroup("Gizmos")] public float vertexRadius = 0.1F;
        
        void OnDrawGizmos() {
            var defaultColor = Gizmos.color;
            if (draw) {
                for (int i = 0; i < waypoints.Count; i++) {
                    if (i == 0) {
                        Gizmos.color = vertexColor;
                        Gizmos.DrawCube(waypoints[i], 2 * vertexRadius * Vector3.one);
                    } else {
                        Gizmos.color = edgeColor;
                        Gizmos.DrawLine(waypoints[i-1], waypoints[i]);
                        Gizmos.color = vertexColor;
                        Gizmos.DrawSphere(waypoints[i], vertexRadius);
                    }
                }
            }
            Gizmos.color = defaultColor;
        }
#endif
    }
}