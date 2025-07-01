using System.Linq;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class PlaymodeLocationBoundsPresenter : MonoBehaviour {
        static readonly int DitheringBoundYMax = Shader.PropertyToID("_DitheringBoundYMax");
        static readonly int DitheringBoundYMin = Shader.PropertyToID("_DitheringBoundYMin");
    
        [InfoBox("White sphere - transform pivot, Yellow sphere - bounds position, Red box - bounds")]
        [ShowInInspector]
        Bounds _bounds;

        [Button("Refresh")]
        public void Refresh() {
            _bounds = TransformBoundsUtil.FindBounds(transform, false);
        
            var modelMaterials = gameObject.GetComponentsInChildren<Renderer>()
                .SelectMany(r => r?.materials)
                .Distinct()
                .Where(s => s != null);
            var yMax = _bounds.max.y;
            var yMin = _bounds.min.y;
            foreach (Material material in modelMaterials) {
                material.SetFloat(DitheringBoundYMax, yMax);
                material.SetFloat(DitheringBoundYMin, yMin);
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position, 0.5f);
            var position = _bounds.center;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(position, _bounds.size);
        }
    }
}
