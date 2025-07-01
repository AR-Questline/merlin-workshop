using System.Runtime.CompilerServices;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Grounds {
    public class MedusaGroundBoundsBaker : MonoBehaviour {
        [Button]
        public void Bake(GroundBounds groundBounds) {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
                var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                UnityEditor.PrefabUtility.UnpackPrefabInstance(root, UnityEditor.PrefabUnpackMode.Completely,
                    UnityEditor.InteractionMode.AutomatedAction);
            }
#endif

            groundBounds.CalculateMedusaPolygon(ARAlloc.Temp, out var medusaPolygon);

            var lodGroups = GetComponentsInChildren<LODGroup>();
            for (int i = 0; i < lodGroups.Length; i++) {
                LODGroup lodGroup = lodGroups[i];
                var groupBounds = MinMaxAABR.Empty;
                var lods = lodGroup.GetLODs();
                CalculateBounds(lods, ref groupBounds);

                Polygon2DUtils.Intersects(groupBounds, medusaPolygon, out var intersects);
                if (!intersects) {
                    DestroyImmediate(lodGroup.gameObject);
                }
            }

            medusaPolygon.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateBounds(LOD[] lods, ref MinMaxAABR groupBounds) {
            for (int i = 0; i < lods.Length; i++) {
                LOD lod = lods[i];
                for (int j = 0; j < lod.renderers.Length; j++) {
                    Renderer renderer = lod.renderers[j];
                    if (renderer is MeshRenderer meshRenderer) {
                        var bounds = new MinMaxAABR(meshRenderer.bounds);
                        groupBounds.Encapsulate(bounds);
                    }
                }
            }
        }
    }
}
