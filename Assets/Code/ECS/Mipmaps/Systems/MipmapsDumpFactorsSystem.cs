using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Graphics.Mipmaps;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.Mipmaps.Systems {
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class MipmapsDumpFactorsSystem : SystemBase, MipmapsStreamingMasterMaterials.IMipmapsFactorProvider {
        MipmapsRegisterMaterialsAndMeshesSystem _mipmapRegisterMaterialsAndMeshes;

        EntityQuery _renderMeshArrayQuery;
        EntityQuery _materialMeshInfoQuery;
        EntityQuery _rangesQuery;

        SharedComponentTypeHandle<RenderMeshArray> _renderMeshArrayHandle;
        SharedComponentTypeHandle<MipmapsMaterialIdsComponent> _mipmapsMaterialIdsComponentHandle;
        ComponentTypeHandle<MipmapsTransformFactorComponent> _mipmapsTransformFactorComponentHandle;
        ComponentTypeHandle<MipmapsFactorComponent> _mipmapsFactorComponentHandle;
        ComponentTypeHandle<MaterialMeshInfo> _materialMeshInfoHandle;

        protected override void OnCreate() {
            _renderMeshArrayQuery = GetEntityQuery(
                ComponentType.ReadOnly<RenderMeshArray>(),
                ComponentType.ReadOnly<MipmapsTransformFactorComponent>(),
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.Exclude<DisableRendering>());

            _materialMeshInfoQuery = GetEntityQuery(
                ComponentType.ReadOnly<MipmapsMaterialComponent>(),
                ComponentType.ReadOnly<MipmapsFactorComponent>(),
                ComponentType.Exclude<DisableRendering>(),
                ComponentType.Exclude<MipmapsMaterialIdsComponent>());

            _rangesQuery = GetEntityQuery(
                ComponentType.ReadOnly<MipmapsMaterialIdsComponent>(),
                ComponentType.ReadOnly<MipmapsFactorComponent>(),
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.Exclude<DisableRendering>(),
                ComponentType.Exclude<MipmapsMaterialComponent>());

            _renderMeshArrayHandle = GetSharedComponentTypeHandle<RenderMeshArray>();
            _mipmapsMaterialIdsComponentHandle = GetSharedComponentTypeHandle<MipmapsMaterialIdsComponent>();
            _mipmapsTransformFactorComponentHandle = GetComponentTypeHandle<MipmapsTransformFactorComponent>(true);
            _mipmapsFactorComponentHandle = GetComponentTypeHandle<MipmapsFactorComponent>(true);
            _materialMeshInfoHandle = GetComponentTypeHandle<MaterialMeshInfo>(true);
        }

        protected override void OnStartRunning() {
            _mipmapRegisterMaterialsAndMeshes = World.GetExistingSystemManaged<MipmapsRegisterMaterialsAndMeshesSystem>();
            MipmapsStreamingMasterMaterials.Instance.AddProvider(this);
        }

        protected override void OnStopRunning() {
            MipmapsStreamingMasterMaterials.Instance.RemoveProvider(this);
        }

        protected override unsafe void OnUpdate() {
            var brgRenderMeshArrays = _mipmapRegisterMaterialsAndMeshes.mipmapsRenderArrays;

            _renderMeshArrayHandle.Update(this);
            _mipmapsMaterialIdsComponentHandle.Update(this);
            _mipmapsTransformFactorComponentHandle.Update(this);
            _mipmapsFactorComponentHandle.Update(this);
            _materialMeshInfoHandle.Update(this);

            var mipmapsWriter = MipmapsStreamingMasterMaterials.Instance.GetParallelWriter();

            SharedComponentInfo* infos = EntityManager.GetAllSharedComponentInfo<RenderMeshArray>();

            var dependency = Dependency;

            var dumpRenderMeshArrayHandle = new DumpRenderMeshArrayFactorsJob {
                mipmapsRenderArrays = brgRenderMeshArrays,
                renderMeshArrayHandle = _renderMeshArrayHandle,
                mipmapsTransformFactorComponentHandle = _mipmapsTransformFactorComponentHandle,
                materialMeshInfoHandle = _materialMeshInfoHandle,
                sharedComponentInfos = infos,
                mipmapsWriter = mipmapsWriter,
            }.ScheduleParallel(_renderMeshArrayQuery, dependency);

            var dumpMaterialMeshHandle = new DumpMaterialMeshInfoJob {
                mipmapsWriter = mipmapsWriter,
            }.ScheduleParallel(_materialMeshInfoQuery, dependency);

            var dumpByRangesHandle = new DumpMaterialMeshJob {
                mipmapsMaterialIdsComponentHandle = _mipmapsMaterialIdsComponentHandle,
                mipmapsFactorHandle = _mipmapsFactorComponentHandle,
                materialMeshInfoHandle = _materialMeshInfoHandle,
                mipmapsWriter = mipmapsWriter,
            }.ScheduleParallel(_rangesQuery, dependency);

            var combinedDependency = JobHandle.CombineDependencies(dumpRenderMeshArrayHandle, dumpMaterialMeshHandle, dumpByRangesHandle);
            Dependency = mipmapsWriter.Dispose(combinedDependency);
        }

        [BurstCompile]
        unsafe partial struct DumpRenderMeshArrayFactorsJob : IJobChunk {
            [ReadOnly] public UnsafeParallelHashMap<int, MipmapsRegisterMaterialsAndMeshesSystem.MipmapsRenderArray> mipmapsRenderArrays;
            [ReadOnly] public SharedComponentTypeHandle<RenderMeshArray> renderMeshArrayHandle;
            [ReadOnly] public ComponentTypeHandle<MipmapsTransformFactorComponent> mipmapsTransformFactorComponentHandle;
            [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> materialMeshInfoHandle;
            [NativeDisableUnsafePtrRestriction] public SharedComponentInfo* sharedComponentInfos;
            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsWriter;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                int renderMeshArrayIndex = chunk.GetSharedComponentIndex(renderMeshArrayHandle);
                if (!mipmapsRenderArrays.TryGetValue(renderMeshArrayIndex, out var mipmapsRenderArray)) {
                    return;
                }
                if (sharedComponentInfos[renderMeshArrayIndex].Version != mipmapsRenderArray.version) {
                    return;
                }

                var transformFactors = chunk.GetNativeArray(ref mipmapsTransformFactorComponentHandle);
                var materialMeshInfos = chunk.GetNativeArray(ref materialMeshInfoHandle);
                var rangeAccess = mipmapsRenderArray.rangesAccess;

                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var materialMeshInfo = materialMeshInfos[i];
                    if (materialMeshInfo.HasMaterialMeshIndexRange) {
                        RangeInt matMeshIndexRange = materialMeshInfos[i].MaterialMeshIndexRange;

                        var factor = transformFactors[i].value;
                        for (int j = matMeshIndexRange.start; j < matMeshIndexRange.end; j++) {
                            var rangeData = rangeAccess[j];

                            var materialId = mipmapsRenderArray.materialIds[rangeData.materialIndex];
                            var reciprocalUvDistribution = mipmapsRenderArray.reciprocalUvDistribution[rangeData.meshIndex];
                            var finalFactor = factor * reciprocalUvDistribution;

                            mipmapsWriter.UpdateMipFactor(materialId, finalFactor);
                        }
                    } else {
                        var materialId = mipmapsRenderArray.materialIds[materialMeshInfos[i].MaterialArrayIndex];
                        var reciprocalUvDistribution = mipmapsRenderArray.reciprocalUvDistribution[materialMeshInfos[i].MeshArrayIndex];
                        var factor = transformFactors[i].value * reciprocalUvDistribution;
                        mipmapsWriter.UpdateMipFactor(materialId, factor);
                    }
                }
            }
        }

        [BurstCompile]
        partial struct DumpMaterialMeshInfoJob : IJobEntity {
            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsWriter;

            public void Execute(in MipmapsMaterialComponent mipmapsMaterial, in MipmapsFactorComponent mipmapsFactor) {
                mipmapsWriter.UpdateMipFactor(mipmapsMaterial.id, mipmapsFactor.value);
            }
        }

        [BurstCompile]
        unsafe partial struct DumpMaterialMeshJob : IJobChunk {
            [ReadOnly] public SharedComponentTypeHandle<MipmapsMaterialIdsComponent> mipmapsMaterialIdsComponentHandle;
            [ReadOnly] public ComponentTypeHandle<MipmapsFactorComponent> mipmapsFactorHandle;
            [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> materialMeshInfoHandle;

            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsWriter;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var mipmapsMaterialIdsComponents = chunk.GetSharedComponent(mipmapsMaterialIdsComponentHandle);

                var mipmapsFactors = chunk.GetNativeArray(ref mipmapsFactorHandle);
                var materialMeshInfos = chunk.GetNativeArray(ref materialMeshInfoHandle);
                var chunkEntityCount = chunk.Count;

                for (int i = 0; i < chunkEntityCount; i++) {
                    var materialMeshInfo = materialMeshInfos[i];
                    var factor = mipmapsFactors[i].value;

                    if (Hint.Likely(materialMeshInfo.HasMaterialMeshIndexRange)) {
                        RangeInt matMeshIndexRange = materialMeshInfo.MaterialMeshIndexRange;
                        for (int j = 0; j < matMeshIndexRange.length; j++) {
                            int matMeshSubMeshIndex = matMeshIndexRange.start + j;
                            var materialId = mipmapsMaterialIdsComponents.ids[matMeshSubMeshIndex];
                            if (Hint.Likely(materialId != MipmapsStreamingMasterMaterials.MaterialId.Invalid)) {
                                mipmapsWriter.UpdateMipFactor(materialId, factor);
                            }
                        }
                    } else {
                        Debug.LogError("Cannot update mipmaps factor for material mesh info without range");
                    }
                }
            }
        }

        public void ProvideMipmapsFactors(in CameraData _, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            writer.Dispose(Dependency);
        }
    }
}
