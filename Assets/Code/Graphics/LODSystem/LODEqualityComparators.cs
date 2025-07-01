using UnityEngine;

namespace Awaken.TG.Graphics.LODSystem {
    public static class LODEqualityComparators {
        [UnityEngine.Scripting.Preserve]
        public static bool LodArrayEquals(LOD[] lod1, LOD[] lod2) {
            if (ReferenceEquals(lod1, lod2)) return true;
            if (lod1 == null || lod2 == null) return false;
            if (lod1.Length != lod2.Length) return false;
            
            for (int i = 0; i < lod1.Length; i++) {
                if (!LodStructEquals(lod1[i], lod2[i])) {
                    return false;
                }
            }
            return true;
        }

        static bool LodStructEquals(LOD lod1, LOD lod2) {
            if (System.Math.Abs(lod1.fadeTransitionWidth - lod2.fadeTransitionWidth) > 0.001f) return false;
            if (System.Math.Abs(lod1.screenRelativeTransitionHeight - lod2.screenRelativeTransitionHeight) > 0.001f) return false;
            return RenderersEquals(lod1.renderers, lod2.renderers);
        }

        static bool RenderersEquals(Renderer[] renderers, Renderer[] renderers2) {
            if (ReferenceEquals(renderers, renderers2)) return true;
            if (renderers == null || renderers2 == null) return false;
            if (renderers.Length != renderers2.Length) return false;
            
            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i] != renderers2[i]) {
                    return false;
                }
            }

            return true;
        }
    }
}