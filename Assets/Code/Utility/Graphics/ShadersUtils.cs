using UnityEngine;

namespace Awaken.Utility.Graphics {
    public static class ShadersUtils {
        public static bool IsMaterialTransparent(Material material) {
            const int SurfaceTypeTransparent = 1; // Corresponds to non-public SurfaceType.Transparent
            if (material == null) {
                return false;
            }
            int surfaceTypeHDRPNameID = Shader.PropertyToID("_SurfaceType");
            if (material.HasProperty(surfaceTypeHDRPNameID)) {
                return (int)material.GetFloat(surfaceTypeHDRPNameID) == SurfaceTypeTransparent;
            } else {
                return false;
            }
        }
    }
}
