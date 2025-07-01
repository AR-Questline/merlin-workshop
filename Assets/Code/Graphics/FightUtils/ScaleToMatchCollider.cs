using System;
using System.Linq;
using Awaken.TG.Utility;
using Awaken.Utility.PhysicUtils;
using UnityEngine;

namespace Awaken.TG.Graphics.FightUtils {
    public class ScaleToMatchCollider : MonoBehaviour {
        public Bounds bounds;

        void Start() {
            Collider firstHit = null;
            foreach (var c in PhysicsQueries.OverlapBox(bounds.center + transform.position, bounds.extents)) {
                if (c is not TerrainCollider) {
                    firstHit = c;
                    break;
                }
            }
            if (firstHit == null) {
                return;
            }
            var targetSize = firstHit.bounds.size;

            float maxScale = targetSize.x / bounds.size.x;
            maxScale = Mathf.Max( targetSize.y / bounds.size.y, maxScale);
            maxScale = Mathf.Max( targetSize.z / bounds.size.z, maxScale);
            
            transform.localScale = Vector3.one*maxScale;
        }

        void OnValidate() {
            if (bounds.size.magnitude < 0.1f) {
                bounds = TransformBoundsUtil.FindBounds(transform, false);
                bounds.center = transform.InverseTransformPoint(bounds.center);
            }
        }

        void OnDrawGizmosSelected() {
            Gizmos.DrawWireCube(bounds.center + transform.position, bounds.size);
        }
    }
}