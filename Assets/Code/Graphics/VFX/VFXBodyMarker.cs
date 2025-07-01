using Awaken.Kandra;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    [AddComponentMenu("VFX/VFX Body Marker")]
    public class VFXBodyMarker : MonoBehaviour {
        [SerializeField] KandraRenderer kandraRenderer;

        int _useCount;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public KandraRenderer Renderer => kandraRenderer;
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public KandraMesh Mesh => Renderer.rendererData.mesh;
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public float BoundsSize => Mesh.localBoundingSphere.w;
        
        void Awake() {
            kandraRenderer.enabled = _useCount > 0;
        }

        public void MarkBeingUsed() {
            _useCount++;
            if (_useCount == 1) {
                kandraRenderer.enabled = true;
            }
        }
        
        public void MarkBeingUnused() {
            _useCount--;
            if (_useCount == 0) {
                kandraRenderer.enabled = false;
            }
        }
        
        public void OnValidate() {
            if (kandraRenderer == null) {
                kandraRenderer = GetComponent<KandraRenderer>();
            }
        }
    }
}