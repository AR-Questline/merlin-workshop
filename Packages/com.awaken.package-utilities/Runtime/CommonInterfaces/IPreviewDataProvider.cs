using UnityEngine;

namespace Awaken.CommonInterfaces {
    public interface IPreviewDataProvider {
#if UNITY_EDITOR
        DrawMeshDatum EDITOR_GetDrawMeshDatum();
#endif
    }
    
    public struct DrawMeshDatum {
        public Bounds localBounds;
        public Mesh mesh;
        public Material[] materials;
        public Matrix4x4 localToWorld;
        public int layer;
    }
}
