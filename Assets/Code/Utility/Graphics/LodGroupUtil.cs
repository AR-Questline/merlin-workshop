using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Awaken.Utility.Graphics {
    /// <summary>
    /// Taken from: https://github.com/JulienHeijmans/EditorScripts/blob/master/Scripts/Utility/Editor/LODExtendedUtility.cs
    /// That was mostly sourced from: https://github.com/Unity-Technologies/AutoLOD/blob/master/Runtime/Extensions/LODGroupExtensions.cs
    /// </summary>
    public static class LodGroupUtil {
        //Return the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null 
        static public LODGroup GetParentLODGroupComponent(GameObject GO) {
            LODGroup lodGroupParent = GO.GetComponentInParent<LODGroup>();
            if (lodGroupParent == null)
                return null;
            LOD[] LODs = lodGroupParent.GetLODs();

            var foundLOD = LODs.Where(lod =>
                                   lod.renderers.Where(renderer => renderer == GO.GetComponent<Renderer>())
                                      .ToArray().Any())
                               .ToArray();
            return foundLOD.Any() ? lodGroupParent : null;
        }

        //Return the GameObject of the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null.
        static public GameObject GetParentLODGroupGameObject(GameObject GO) {
            var LODGroup = GetParentLODGroupComponent(GO);

            return LODGroup == null ? null : LODGroup.gameObject;
        }

        //Get the LOD # of a selected GameObject. If the GameObject is not part of any LODGroup returns -1.
        static public int GetLODid(GameObject go) {
            LODGroup lodGroupParent = go.GetComponentInParent<LODGroup>();
            if (lodGroupParent == null)
                return -1;
            LOD[] LODs = lodGroupParent.GetLODs();

            var index = Array.FindIndex(LODs,
                lod => lod.renderers.Where(renderer => renderer == go.GetComponent<Renderer>()).ToArray().Any());
            return index;
        }

        //returns the currently visible LOD level of a specific LODGroup, from a specific camera. If no camera is define, uses the Camera.current.
        public static int GetVisibleLOD(LODGroup lodGroup, Camera camera = null) => GetVisibleLOD(lodGroup, lodGroup.GetLODs(), camera);

        static Transform s_camTransform;
        static Camera s_cam;
        public static int GetVisibleLOD(LODGroup lodGroup, LOD[] lods, Camera camera) {
            if (camera != s_cam) {
                s_camTransform = camera.transform;
                s_cam = camera;
            }
            var relativeHeight = GetRelativeHeight(lodGroup);

            int lodIndex = lodGroup.lodCount;
            for (var i = 0; i < lods.Length; i++) {
                var lod = lods[i];

                if (relativeHeight >= lod.screenRelativeTransitionHeight) {
                    lodIndex = i;
                    break;
                }
            }
            
            return lodIndex;
        }

#if UNITY_EDITOR
        //returns the currently visible LOD level of a specific LODGroup, from a the SceneView Camera.
        public static int GetVisibleLODSceneView(LODGroup lodGroup) {
            Camera camera = UnityEditor.SceneView.lastActiveSceneView.camera;
            return GetVisibleLOD(lodGroup, camera);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetRelativeHeight(LODGroup lodGroup) {
            Transform lodGroupTransform = lodGroup.transform;
            var distance = (lodGroupTransform.TransformPoint(lodGroup.localReferencePoint) -
                            s_camTransform.position).magnitude;
            return DistanceToRelativeHeight((distance / QualitySettings.lodBias),
                GetWorldSpaceSize(lodGroupTransform, lodGroup.size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float DistanceToRelativeHeight(float distance, float size) {
            if (s_cam.orthographic) {
                return size * 0.5F / s_cam.orthographicSize;
            }

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * s_cam.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxLOD(LODGroup lodGroup) {
            return lodGroup.lodCount - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetWorldSpaceSize(Transform lodGroup, float size) {
            return GetWorldSpaceScale(lodGroup) * size;
        }

        public static bool Has(this LODGroup lodGroup, Renderer renderer) {
            var lods = lodGroup.GetLODs();
            foreach (var lod in lods) {
                foreach (var lodRenderer in lod.renderers) {
                    if(lodRenderer == renderer) {
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetWorldSpaceScale(Transform t) {
            var scale = t.lossyScale;
            float largestAxis = scale.x;
            largestAxis = Mathf.Max(largestAxis, scale.y);
            largestAxis = Mathf.Max(largestAxis, scale.z);
            return largestAxis;
        }
    }
}