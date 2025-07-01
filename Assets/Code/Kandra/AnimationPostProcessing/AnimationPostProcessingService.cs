using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UniversalProfiling;

namespace Awaken.Kandra.AnimationPostProcessing {
    public static class AnimationPostProcessingService {
        public const int BatchSize = 8;
        
        static readonly UniversalProfilerMarker RemovalMarker = new("AnimationPostProcessingService.Removal");
        static readonly UniversalProfilerMarker AdditionMarker = new("AnimationPostProcessingService.Addition");

        static bool s_exitingPlayMode;
        
        [BurstCompile]
        struct AnimationPostProcessingJob : IJobParallelForTransform {
            [ReadOnly] public NativeList<Vector3> positions;
            [ReadOnly] public NativeList<Vector3> scales;

            public void Execute(int index, TransformAccess transform) {
                if (!transform.isValid) {
                    return;
                }

                transform.localPosition = positions[index];
                transform.localScale = scales[index];
            }
        }

        // Burst data
        static TransformAccessArray s_transformAccess;
        static NativeList<Vector3> s_positions;
        static NativeList<Vector3> s_scales;
        static NativeList<int> s_freeIds;

        // Jobs
        static AnimationPostProcessingJob s_job;
        static JobHandle s_handle;

        static int s_transformationCount = 0;

        public static JobHandle JobHandle => s_handle;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init() {
            const int InitialCapacity = 3000;

            s_transformAccess = new TransformAccessArray(InitialCapacity);
            s_positions = new NativeList<Vector3>(InitialCapacity, ARAlloc.Persistent);
            s_scales = new NativeList<Vector3>(InitialCapacity, ARAlloc.Persistent);
            s_freeIds = new NativeList<int>(InitialCapacity / BatchSize, ARAlloc.Persistent);

            s_job = new AnimationPostProcessingJob {
                positions = s_positions,
                scales = s_scales
            };

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += state => {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                    Dispose();
                    s_exitingPlayMode = true;
                }
                if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) {
                    s_exitingPlayMode = false;
                }
            };
            
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += _ => Dispose();
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            UnityEditor.EditorApplication.quitting += Dispose;
#else
            UnityEngine.Application.quitting += Dispose;
#endif
        }

        static void Dispose() {
            if (!s_transformAccess.isCreated) return;
            s_transformAccess.Dispose();
            s_positions.Dispose();
            s_scales.Dispose();
            s_freeIds.Dispose();
            s_job = default;
            s_handle = default;
            s_transformationCount = 0;
        }

        // === PlayerLoop Entry points
        public static void BeginJob() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                s_handle = default;
                return;
            }
#endif
            if (s_transformationCount == 0) {
                s_handle = default;
                return;
            }

            s_handle = s_job.ScheduleByRef(s_transformAccess);
        }
        
        // === Public Registration

        public static void Register(AnimationPostProcessing npcProcessing) {
            if (s_exitingPlayMode) {
                return;
            }
            
            if (npcProcessing.batchStartIndex.IsNullOrEmpty()) {
                return;
            }
            
            if (!s_transformAccess.isCreated) {
                Log.Critical?.Error($"Trying to register AnimationPostProcessing {npcProcessing} to uninitialized service!", npcProcessing);
                return;
            }
            if (npcProcessing.batchStartIndex[0] != -1) {
                Log.Critical?.Error($"Trying to register AnimationPostProcessing {npcProcessing} that is already registered", npcProcessing);
                return;
            }

            AdditionMarker.Begin();

            int batchCount = npcProcessing.batchStartIndex.Length;
            int toAdd = BatchSize * batchCount;
            EnsureCapacity(s_transformationCount + toAdd);
            s_transformationCount += toAdd;

            for (int batchID = 0; batchID < batchCount; batchID++) {
                RegisterBatch(npcProcessing, batchID);
            }

            AdditionMarker.End();
        }

        public static void Unregister(AnimationPostProcessing oldProcessing) {
            if (s_exitingPlayMode) {
                return;
            }
            
            if (oldProcessing.batchStartIndex.IsNullOrEmpty()) {
                return;
            }
            
            if (!s_transformAccess.isCreated) {
                Log.Critical?.Error($"Trying to unregister AnimationPostProcessing {oldProcessing} from uninitialized service!", oldProcessing);
                return;
            }
            if (oldProcessing.batchStartIndex[0] == -1) {
                Log.Critical?.Error($"Trying to unregister AnimationPostProcessing {oldProcessing} that is not registered", oldProcessing);
                return;
            }

            RemovalMarker.Begin();

            int batchCount = oldProcessing.batchStartIndex.Length;
            int toRemove = BatchSize * batchCount;
            for (int batchId = 0; batchId < batchCount; batchId++) {
                int index = oldProcessing.batchStartIndex[batchId];
                s_freeIds.Add(index);
                
                for (int i = 0; i < BatchSize; i++) {
                    s_transformAccess[index + i] = null;
                }
            }
            s_transformationCount -= toRemove;
            Array.Fill(oldProcessing.batchStartIndex, -1);
            
            RemovalMarker.End();
        }
        
        // === Registration internals
        
        static unsafe void RegisterBatch(AnimationPostProcessing npcProcessing, int batchID) {
            if (s_freeIds.Length > 0) {
                int lastFreeIDIndex = s_freeIds.Length - 1;
                int index = s_freeIds[lastFreeIDIndex];
                s_freeIds.RemoveAt(lastFreeIDIndex);

                npcProcessing.batchStartIndex[batchID] = index;

                for (int i = 0; i < BatchSize; i++) {
                    s_transformAccess[index + i] = npcProcessing.transforms[batchID * BatchSize + i];
                }

                CopyBatchToPosition(s_positions, index, npcProcessing.positions, batchID * BatchSize, BatchSize);
                CopyBatchToPosition(s_scales, index, npcProcessing.scales, batchID * BatchSize, BatchSize);
            } else {
                npcProcessing.batchStartIndex[batchID] = s_positions.Length;
                
                for (int i = 0; i < BatchSize; i++) {
                    s_transformAccess.Add(npcProcessing.transforms[batchID * BatchSize + i]);
                }

                fixed (Vector3* ptr = npcProcessing.positions) {
                    s_positions.AddRangeNoResize(ptr + batchID * BatchSize, BatchSize);
                }

                fixed (Vector3* ptr = npcProcessing.scales) {
                    s_scales.AddRangeNoResize(ptr + batchID * BatchSize, BatchSize);
                }
            }
        }

        static unsafe void CopyBatchToPosition<T>(NativeList<T> destination, int destinationOffset, T[] source, int sourceOffset, int count) where T : unmanaged {
            fixed (T* ptr = source) {
                UnsafeUtility.MemCpy(destination.GetUnsafeList()->Ptr + destinationOffset, ptr + sourceOffset, count * sizeof(T));
            }
        }
        
        static void EnsureCapacity(int capacity) {
            if (capacity <= s_positions.Capacity) return;

            s_transformAccess.capacity = capacity;
            s_positions.Capacity = capacity;
            s_scales.Capacity = capacity;

            Log.Critical?.Error($"AnimationPostProcessingService capacity increased to {s_transformationCount}");
        }
    }
}