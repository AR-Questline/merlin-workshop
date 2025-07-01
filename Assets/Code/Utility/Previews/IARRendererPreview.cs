using UnityEngine;

namespace Awaken.Utility.Previews {
    public interface IARRendererPreview {
        bool IsValid { get; }
        Mesh Mesh { get; }
        Material[] Materials { get; }
        Bounds WorldBounds { get; }
        Matrix4x4 Matrix { get; }

        void Dispose();
    }
}