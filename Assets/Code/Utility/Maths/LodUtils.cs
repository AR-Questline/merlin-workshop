using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class LodUtils {
        public static int LodMask(LOD[] lods, Renderer renderer) {
            int mask = 0;
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++) {
                LOD lod = lods[lodIndex];
                foreach (Renderer lodRenderer in lod.renderers) {
                    if (lodRenderer == renderer) {
                        mask |= 1 << lodIndex;
                    }
                }
            }
            return mask;
        }

        public static float GetWorldSpaceScale(Transform transform) {
            var scale = transform.lossyScale;
            return GetWorldSpaceScale(scale);
        }

        public static float GetWorldSpaceScale(float4x4 transform) {
            var scale = transform.Scale();
            return GetWorldSpaceScale(scale);
        }

        public static float GetWorldSpaceScale(float3 lossyScale) {
            return math.cmax(math.abs(lossyScale));
        }
    }
}
