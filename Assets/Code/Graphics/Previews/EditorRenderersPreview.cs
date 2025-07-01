using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Awaken.Utility.Maths;
using UnityEngine;

#if UNITY_EDITOR
using Awaken.CommonInterfaces;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Awaken.TG.Graphics.Previews {
    public static class EditorRenderersPreview {
#if UNITY_EDITOR
        const int VisibleDistance = 40;
        const int VisibleDistanceSq = VisibleDistance * VisibleDistance;
        
        static readonly HashSet<PreviewHolder> PreviewHolders = new();
        static readonly Plane[] Planes = new Plane[6];
        static readonly List<PreviewHolder> PreviouslySelectedHolders = new();
        static PreviewHolder s_toSelect;
        static List<DrawMeshDatum> s_drawMeshData = new List<DrawMeshDatum>(32);

        [InitializeOnLoadMethod]
        static void Setup() {
            var delegateMethod = typeof(EditorRenderersPreview).GetMethod(nameof(PickPreviewedGameObject));
            var delegateType = typeof(HandleUtility).GetNestedType("PickClosestGameObjectFunc", BindingFlags.NonPublic);
            var delegateField = typeof(HandleUtility).GetField("pickClosestGameObjectDelegate", BindingFlags.Static | BindingFlags.NonPublic);
            delegateField!.SetValue(null, Delegate.CreateDelegate(delegateType, delegateMethod!));
            
            SceneView.beforeSceneGui -= BeforeSceneGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;
            SceneView.duringSceneGui -= SceneGUI;
            SceneView.duringSceneGui += SceneGUI;
        }
#endif
        
        [Conditional("UNITY_EDITOR")]
        public static void Register(IWithRenderersToPreview toPreview) {
#if UNITY_EDITOR
            PreviewHolders.Add(new PreviewHolder(toPreview));
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void Unregister(IWithRenderersToPreview toPreview) {
#if UNITY_EDITOR
            PreviewHolders.RemoveWhere(holder => holder.toPreview == toPreview);
#endif
        }
        
#if UNITY_EDITOR
        public static GameObject PickPreviewedGameObject(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex) {
            materialIndex = -1;
            return TryPickHolder(cam.ScreenPointToRay(position), ignore, filter, out var holder)
                ? holder.toPreview.PreviewParent
                : null;
        }
        
        static void BeforeSceneGUI(SceneView sceneView) {
            if (EditorPrefs.GetBool("disableAllPreviews", false)) {
                return;
            }
            
            var currentEvent = Event.current;
            if (currentEvent.type is EventType.MouseMove or EventType.MouseDrag) {
                PreviouslySelectedHolders.Clear();
            }
        }
        
        static void SceneGUI(SceneView sceneView) {
            if (EditorPrefs.GetBool("disableAllPreviews", false) || Application.isPlaying) {
                return;
            }
            
            if (Event.current.type == EventType.Repaint) {
                // .IsValid fairly expensive, so may need to be optimized in future
                PreviewHolders.RemoveWhere(static holder => !holder.toPreview.IsValid);

                var sceneCamera = sceneView.camera;
                var cameraPosition = sceneCamera.transform.position;
                var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
                GeometryUtility.CalculateFrustumPlanes(sceneCamera, Planes);
                
                var isDisabledCache = new Dictionary<string, bool>();
                s_drawMeshData.Clear();
                foreach (var holder in PreviewHolders) {
                    var disabledKey = holder.toPreview.DisablePreviewKey;
                    if (!isDisabledCache.TryGetValue(disabledKey, out var isDisabled)) {
                        isDisabled = EditorPrefs.GetBool(disabledKey, false);
                        isDisabledCache.Add(disabledKey, isDisabled);
                    }
                    if (!isDisabled) {
                        DrawPreview(holder, currentStage, sceneCamera, cameraPosition);
                    }
                }
            }
        }
        
        static bool TryPickHolder(Ray ray, GameObject[] ignore, GameObject[] filter, out PreviewHolder pickedHolder) {
            pickedHolder = null;
            foreach (var holder in PreviewHolders) {
                if (CanSelect(holder, ray, ignore, filter)) {
                    PreviouslySelectedHolders.Add(holder);
                    pickedHolder = holder;
                    return true;
                }
            }
            return false;
        }

        static bool CanSelect(PreviewHolder holder, Ray ray, GameObject[] ignore, GameObject[] filter) {
            var go = holder.toPreview.PreviewParent;
            var worldToLocal = go.transform.worldToLocalMatrix;
            var localRay = new Ray(worldToLocal.MultiplyPoint(ray.origin), worldToLocal.MultiplyVector(ray.direction));
            return InexactIntersecting(holder, localRay) && AllowSelectionByFilters(holder, ignore, filter) && ExactIntersecting(holder, localRay);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InexactIntersecting(PreviewHolder holder, Ray localRay) {
            return holder.localBoundsSum.IntersectRay(localRay, out float distance) && 
                distance < VisibleDistance && 
                (distance * distance) < holder.localBoundsSum.size.sqrMagnitude * 820;
            // cot(2deg)^2 ~= 820
            // doesn't intersects if bounds take space less than 2deg of FOV
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool AllowSelectionByFilters(PreviewHolder holder, GameObject[] ignore, GameObject[] filter) {
            return !PreviouslySelectedHolders.Contains(holder) &&
                   (ignore == null || !ignore.Contains(holder.toPreview.PreviewParent)) &&
                   (filter == null || filter.Contains(holder.toPreview.PreviewParent));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ExactIntersecting(PreviewHolder holder, Ray localRay) {
            if (holder.toPreview.TryGetRenderers(out var meshRenderers, out var skinnedMeshRenderers, out var providers)) {
                foreach (var renderer in meshRenderers) {
                    if (renderer.bounds.IntersectRay(localRay)) {
                        return true;
                    }
                }
                foreach (var renderer in skinnedMeshRenderers) {
                    if (renderer.bounds.IntersectRay(localRay)) {
                        return true;
                    }
                }
                foreach (var provider in providers) {
#if UNITY_EDITOR
                    var datum = provider.EDITOR_GetDrawMeshDatum();
                    if (datum.localBounds.IntersectRay(localRay)) {
                        return true;
                    }
#endif
                }
            }
            return false;
        }

        static void DrawPreview(PreviewHolder holder, PrefabStage prefabStage, Camera sceneCamera, Vector3 cameraPosition) {
            if (prefabStage != null && !holder.toPreview.CanBeRenderInPrefabStage(prefabStage)) {
                return;
            }
            
            // To optimize furthermore, should use TransformAccessArray and jobified culling
            var parentTransform = holder.toPreview.PreviewParent.transform;
            if (parentTransform.hasChanged) {
                holder.parentLocalToWorld = parentTransform.localToWorldMatrix;
                parentTransform.hasChanged = false;
            }
            Matrix4x4 parentLocalToWorld = holder.parentLocalToWorld;
            
            var position = parentLocalToWorld.GetPosition();
            if ((position - cameraPosition).sqrMagnitude > VisibleDistanceSq) {
                return;
            }

            if (!GeometryUtility.TestPlanesAABB(Planes, holder.localBoundsSum.Transform(parentLocalToWorld))) {
                return;
            }

            Bounds? localBoundsSum = default;
            
            if (holder.toPreview.TryGetDrawMeshData(parentLocalToWorld, s_drawMeshData)) {
                foreach (var drawMeshDatum in s_drawMeshData) {
                    localBoundsSum.Encapsulate(drawMeshDatum.localBounds);
                    for (int i = 0; i < drawMeshDatum.materials.Length; i++) {
                        var material = drawMeshDatum.materials[i];
                        UnityEngine.Graphics.DrawMesh(drawMeshDatum.mesh, drawMeshDatum.localToWorld, material,
                            drawMeshDatum.layer, sceneCamera, i);
                    }
                }
            }
            s_drawMeshData.Clear();
            
            holder.localBoundsSum = localBoundsSum ?? default;
            holder.localBoundsSum.extents = Vector3.Max(holder.localBoundsSum.extents, Vector3.one * 0.05f);
        }
        
        class PreviewHolder {
            public readonly IWithRenderersToPreview toPreview;
            public Bounds localBoundsSum;
            public Matrix4x4 parentLocalToWorld;
            
            public PreviewHolder(IWithRenderersToPreview toPreview) {
                this.toPreview = toPreview;
                parentLocalToWorld = toPreview.PreviewParent.transform.localToWorldMatrix;
            }

            public bool Equals(PreviewHolder other) {
                return toPreview == other.toPreview;
            }

            public override bool Equals(object obj) {
                return obj is PreviewHolder other && Equals(other);
            }

            public override int GetHashCode() {
                return toPreview != null ? toPreview.GetHashCode() : 0;
            }
        }
#endif
    }
}
