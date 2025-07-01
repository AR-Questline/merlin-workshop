using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {
    [Serializable]
    public class GridField {
        [SerializeField] float width = 10;
        [SerializeField] float spacing = 1;
        [SerializeField] int gizmoPoints = 50;

        public List<Vector3> GetSlots(Transform parent, int count) {
            var result = new List<Vector3>();
            var currentSlot = new Vector3(spacing/2, 0, spacing/2);
            
            do {
                do {
                    result.Add(parent.rotation * currentSlot + parent.position);
                    count--;
                    currentSlot += new Vector3(spacing, 0, 0);
                } while (currentSlot.x < width && count > 0);

                currentSlot += new Vector3(0, 0, spacing);
                currentSlot.x = spacing / 2;
            } while (count > 0);

            return result;
        }
        
#if UNITY_EDITOR
        public void DrawSlots(Transform parent) {
            List<Vector3> points = GetSlots(parent, gizmoPoints);

            Color defaultColor = Gizmos.color;
            Gizmos.color = GetGizmosColor(parent.GetInstanceID());
            foreach (Vector3 point in points) {
                Gizmos.DrawSphere(point, 0.25f);
            }

            Gizmos.color = defaultColor;
        }
        
        Color GetGizmosColor(int instanceID) {
            var rng = new System.Random(instanceID);
            return new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), 0.35f);
        }
#endif
    }
}