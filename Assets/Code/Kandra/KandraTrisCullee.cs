using System;
using Awaken.Kandra.Managers;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Kandra {
    [RequireComponent(typeof(KandraRenderer)), BurstCompile, ExecuteInEditMode]
    public class KandraTrisCullee : MonoBehaviour {
        [InlineProperty] public SerializableGuid id;
        public KandraRenderer kandraRenderer;

        UnsafeBitmask _visibleTris;
        [FoldoutGroup("Debug"), ShowInInspector, Sirenix.OdinInspector.ReadOnly] StructList<KandraTrisCuller> _cullers = new StructList<KandraTrisCuller>(8);

        bool _inUnityLifetime;

        void Awake() {
            _inUnityLifetime = true;
            KandraRendererManager.Instance.StopTracking(this);
            KandraEditorGuards.CulleeAwaken(this);
        }

        void OnEnable() {
            kandraRenderer.EnsureInitialized();
            UpdateCulledMesh();
            KandraEditorGuards.CulleeEnabled(this);
        }

        public void OnDisable() {
            var canDisable = true;
            KandraEditorGuards.CanCulleeDisable(this, ref canDisable);
            if (canDisable) {
                kandraRenderer.ReleaseCullableMesh();
                KandraEditorGuards.CulleeDisabled(this);
            }
        }

        public void OnDestroy() {
            var canDestroy = true;
            KandraEditorGuards.CanCulleeDestroy(this, ref canDestroy);
            if (canDestroy) {
                if (_visibleTris.IsCreated) {
                    _visibleTris.Dispose();
                }
                _cullers.Clear();
                KandraEditorGuards.CulleeDestroyed(this);
            }
        }

#if UNITY_EDITOR
        void Reset() {
            if (id == default) {
                id = new SerializableGuid(Guid.NewGuid());
            }
        }
#endif

        [FoldoutGroup("Debug"), Button]
        public void Cull(KandraTrisCuller culler) {
            if (_cullers.Contains(culler)) {
                Log.Critical?.Error("Trying to cull a culler that was already culled");
                return;
            }
            _cullers.Add(culler);

            if (isActiveAndEnabled) {
                UpdateCulledMesh();
            }

            if (!_inUnityLifetime) {
                KandraRendererManager.Instance.StartTracking(this);
            }
        }

        [FoldoutGroup("Debug"), Button]
        public void Uncull(KandraTrisCuller culler) {
            if (!kandraRenderer.rendererData.mesh) {
                return;
            }
            var removed = _cullers.Remove(culler);
            if (!removed) {
                Log.Important?.Warning("Trying to uncull a culler that was not culled");
                return;
            }

            if (this && isActiveAndEnabled) {
                UpdateCulledMesh();
            }
        }

        [FoldoutGroup("Debug"), Button]
        unsafe void UpdateCulledMesh() {
            if (_cullers.Count == 0) {
                kandraRenderer.ReleaseCullableMesh();
                return;
            }

            if (!_visibleTris.IsCreated) {
                CreateVisibleTris();
            }

            _visibleTris.All();
            foreach (var culler in _cullers) {
                culler.DisableCulledTriangles(id, ref _visibleTris);
            }

            var trisCount = _visibleTris.CountOnes();
            var indicesCount = trisCount * 3;

            if (indicesCount == kandraRenderer.rendererData.mesh.indicesCount) {
#if UNITY_EDITOR
                Log.Minor?.Warning($"{this} has cullers but they are not culling anything");
#endif
                kandraRenderer.ReleaseCullableMesh();
            } else {
                var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraRenderer.rendererData.mesh);
                var originalSubmeshes = kandraRenderer.rendererData.mesh.submeshes;
                var newSubmeshes = new UnsafeArray<SubmeshData>((uint)originalSubmeshes.Length, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                fixed (SubmeshData* originalSubmeshesPtr = &originalSubmeshes[0]) {
                    UnsafeUtility.MemCpy(newSubmeshes.Ptr, originalSubmeshesPtr, originalSubmeshes.Length * sizeof(SubmeshData));
                }
                var culledIndices = new UnsafeArray<ushort>(indicesCount, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);
                FillNewIndices(indices.Ptr, culledIndices, _visibleTris, ref newSubmeshes);

                kandraRenderer.UpdateCullableMesh(culledIndices, newSubmeshes);
                culledIndices.Dispose();
            }
        }

        void CreateVisibleTris() {
            var trianglesCount = kandraRenderer.rendererData.mesh.indicesCount / 3;
            _visibleTris = new UnsafeBitmask(trianglesCount, ARAlloc.Persistent);
        }

        [BurstCompile]
        static unsafe void FillNewIndices(ushort* originalIndices, in UnsafeArray<ushort> culledIndices, in UnsafeBitmask visibleTris, ref UnsafeArray<SubmeshData> submeshes) {
            var originalTrianglesPtr = (Triangle*)originalIndices;
            var culledTrianglesPtr = (Triangle*)culledIndices.Ptr;
            var i = 0u;

            var currentSubmesh = 0u;
            var currentSubmeshEnd = submeshes[currentSubmesh].indexCount + submeshes[currentSubmesh].indexStart;
            var currentSubmeshCount = 0u;
            var currentSubmeshStart = 0u;

            foreach (var triangleIndex in visibleTris.EnumerateOnes()) {
                if (triangleIndex*3 >= currentSubmeshEnd) {
                    submeshes[currentSubmesh].indexCount = currentSubmeshCount;

                    currentSubmesh++;
                    currentSubmeshEnd = submeshes[currentSubmesh].indexCount + submeshes[currentSubmesh].indexStart;
                    submeshes[currentSubmesh].indexStart = i * 3;
                    currentSubmeshCount = 0;
                }
                culledTrianglesPtr[i++] = originalTrianglesPtr[triangleIndex];
                currentSubmeshCount += 3;
            }

            submeshes[currentSubmesh].indexCount = currentSubmeshCount;
        }

        struct Triangle {
            ushort i1;
            ushort i2;
            ushort i3;
        }

        public struct EditorAccess {
            public static UnsafeBitmask GetVisibleTriangles(KandraTrisCullee cullee, Allocator allocator) {
                var trianglesCount = cullee.kandraRenderer.rendererData.mesh.indicesCount / 3;
                var visibleTris = new UnsafeBitmask(trianglesCount, allocator);
                visibleTris.All();
                foreach (var culler in cullee._cullers) {
                    culler.DisableCulledTriangles(cullee.id, ref visibleTris);
                }
                return visibleTris;
            }
        }
    }
}