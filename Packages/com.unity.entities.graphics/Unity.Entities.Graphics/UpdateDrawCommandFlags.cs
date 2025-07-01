using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    [BurstCompile]
    internal unsafe struct UpdateDrawCommandFlagsJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
        [ReadOnly] public SharedComponentTypeHandle<RenderFilterSettings> RenderFilterSettings;
        public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;

        [ReadOnly] public NativeParallelHashMap<FilterSettingsIndex, BatchFilterSettings> FilterSettings;
        public BatchFilterSettings DefaultFilterSettings;

#if UNITY_EDITOR
        [ReadOnly] public SharedComponentTypeHandle<EditorRenderData> EditorDataComponentHandle;
#endif

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // This job is not written to support queries with enableable component types.
            Assert.IsFalse(useEnabledMask);

            var chunkInfo = chunk.GetChunkComponentData(ref EntitiesGraphicsChunkInfo);
            Assert.IsTrue(chunkInfo.Valid, "Attempted to process a chunk with uninitialized Hybrid chunk info");

            var localToWorld = chunk.GetNativeArray(ref LocalToWorld);

            // This job runs for all chunks that have structural changes, so if different
            // RenderFilterSettings get set on entities, they should be picked up by
            // the order change filter.
            FilterSettingsIndex filterIndex = new FilterSettingsIndex(chunk.GetSharedComponentIndex(RenderFilterSettings));
#if UNITY_EDITOR
            if (chunk.Has<EditorRenderData>()) {
                int editorDataIndex = chunk.GetSharedComponentIndex(EditorDataComponentHandle);
                filterIndex = filterIndex.WithEditorData(editorDataIndex);
            }
#endif
            if (!FilterSettings.TryGetValue(filterIndex, out var filterSettings))
                filterSettings = DefaultFilterSettings;

            bool hasPerObjectMotion = filterSettings.motionMode != MotionVectorGenerationMode.Camera;
            if (hasPerObjectMotion)
                chunkInfo.CullingData.Flags |= EntitiesGraphicsChunkCullingData.kFlagPerObjectMotion;
            else
                chunkInfo.CullingData.Flags &= unchecked((byte)~EntitiesGraphicsChunkCullingData.kFlagPerObjectMotion);

            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {
                bool flippedWinding = RequiresFlippedWinding(localToWorld[i]);

                int qwordIndex = i / 64;
                int bitIndex = i % 64;
                ulong mask = 1ul << bitIndex;

                if (flippedWinding)
                    chunkInfo.CullingData.FlippedWinding[qwordIndex] |= mask;
                else
                    chunkInfo.CullingData.FlippedWinding[qwordIndex] &= ~mask;
            }

            chunk.SetChunkComponentData(ref EntitiesGraphicsChunkInfo, chunkInfo);
        }

        private bool RequiresFlippedWinding(LocalToWorld localToWorld)
        {
            return math.determinant(localToWorld.Value) < 0.0;
        }
    }
}
