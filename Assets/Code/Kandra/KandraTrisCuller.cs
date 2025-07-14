using System;
using Awaken.Kandra.Managers;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    public class KandraTrisCuller : MonoBehaviour {
        public CulledMesh[] culledMeshes;

        FrugalList<KandraTrisCullee> _cullees = new FrugalList<KandraTrisCullee>();

        bool _inUnityLifetime;

        void Awake() {
            _inUnityLifetime = true;
            KandraRendererManager.Instance.StopTracking(this);
            KandraEditorGuards.CullerAwaken(this);
        }

        public void OnDestroy() {
            var canDestroy = true;
            KandraEditorGuards.CanCullerDestroy(this, ref canDestroy);
            if (canDestroy) {
                foreach (var cullee in _cullees) {
                    cullee.Uncull(this);
                }
                _cullees.Clear();
                KandraEditorGuards.CullerDestroyed(this);
            }
        }

        public void Cull(KandraTrisCullee cullee) {
            cullee.Cull(this);
            _cullees.Add(cullee);

            if (!_inUnityLifetime) {
                KandraRendererManager.Instance.StartTracking(this);
            }
        }

        public void Uncull(KandraTrisCullee cullee) {
            if (_cullees.Remove(cullee)) {
                cullee.Uncull(this);
            }
        }

        public unsafe void DisableCulledTriangles(Guid culleeGuid, ref UnsafeBitmask visibleTris) {
            var culledMesh = Array.Find(culledMeshes, mesh => mesh.culleeId == culleeGuid);
            if (culledMesh.culleeId == Guid.Empty) {
                Log.Minor?.Warning("Trying to disable culled triangles for a cullee that was not culled");
                return;
            }
            var culledRanges = culledMesh.culledRanges;
            fixed (CulledRange* culledRangesPtr = culledRanges) {
                new DisableCulledTrianglesJob {
                    visibleTris = visibleTris,
                    culledRanges = culledRangesPtr,
                    culledRangesLength = culledRanges.Length
                }.Run();
            }
        }

        [Serializable]
        public struct CulledMesh {
            public SerializableGuid culleeId;
            public CulledRange[] culledRanges;
        }

        [Serializable]
        public struct CulledRange {
            public uint start;
            public ushort length;
        }

        [BurstCompile]
        unsafe struct DisableCulledTrianglesJob : IJob {
            public UnsafeBitmask visibleTris;
            [NativeDisableUnsafePtrRestriction] public CulledRange* culledRanges;
            public int culledRangesLength;

            public void Execute() {
                for (var i = 0; i < culledRangesLength; ++i) {
                    var range = culledRanges[i];
                    visibleTris.Down(range.start, range.length);
                }
            }
        }

#if UNITY_EDITOR
        public struct EditorAccess {
            public static ref readonly FrugalList<KandraTrisCullee> Cullees(KandraTrisCuller culler) => ref culler._cullees;
        }
#endif
    }
}