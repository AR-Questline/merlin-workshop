using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    partial class LightProbeUpdateSystem : SystemBase
    {
        EntityQuery m_ProbeGridQuery;

        private ComponentType[] gridQueryFilter = {ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadWrite<BlendProbeTag>()};
        private ComponentType[] gridQueryFilterForAmbient = { ComponentType.ReadWrite<BlendProbeTag>() };

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_ProbeGridQuery = GetEntityQuery(
                ComponentType.ReadWrite<BuiltinMaterialPropertyUnity_SHCoefficients>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<BlendProbeTag>()
            );
            m_ProbeGridQuery.SetChangedVersionFilter(gridQueryFilter);
        }

        internal static bool IsValidLightProbeGrid()
        {
            var probes = LightmapSettings.lightProbes;
            bool validGrid = probes != null && probes.count > 0;
            return validGrid;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (IsValidLightProbeGrid())
            {
                UpdateEntitiesFromGrid();
            }
        }

        private static void UpdateEntitiesFromAmbientProbe(
            LightProbeUpdateSystem system,
            EntityQuery query,
            ComponentType[] queryFilter,
            SphericalHarmonicsL2 ambientProbe,
            SphericalHarmonicsL2 lastProbe)
        {
            Profiler.BeginSample("UpdateEntitiesFromAmbientProbe");
            var updateAll = ambientProbe != lastProbe;
            if (updateAll)
            {
                query.ResetFilter();
            }

            var job = new UpdateSHValuesJob
            {
                Coefficients = new SHCoefficients(ambientProbe),
                SHType = system.GetComponentTypeHandle<BuiltinMaterialPropertyUnity_SHCoefficients>(),
            };

            system.Dependency = job.ScheduleParallel(query, system.Dependency);

            if (updateAll)
            {
                query.SetChangedVersionFilter(queryFilter);
            }
            Profiler.EndSample();
        }

        private List<Vector3> m_Positions = new List<Vector3>(512);
        private List<SphericalHarmonicsL2> m_LightProbes = new List<SphericalHarmonicsL2>(512);
        private List<Vector4> m_OcclusionProbes = new List<Vector4>(512);
        private void UpdateEntitiesFromGrid()
        {
            Profiler.BeginSample("UpdateEntitiesFromGrid");

            var SHType = GetComponentTypeHandle<BuiltinMaterialPropertyUnity_SHCoefficients>();
            var localToWorldType = GetComponentTypeHandle<LocalToWorld>();

            var chunks  = m_ProbeGridQuery.ToArchetypeChunkArray(Allocator.Temp);
            if (chunks.Length == 0)
            {
                Profiler.EndSample();
                return;
            }

            //TODO: Bring this off the main thread when we have new c++ API
            Dependency.Complete();

            foreach (var chunk in chunks)
            {
                var chunkSH = chunk.GetNativeArray(ref SHType);
                var chunkLocalToWorld = chunk.GetNativeArray(ref localToWorldType);

                m_Positions.Clear();
                m_LightProbes.Clear();
                m_OcclusionProbes.Clear();

                for (int i = 0; i != chunkLocalToWorld.Length; i++)
                    m_Positions.Add(chunkLocalToWorld[i].Position);

                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(m_Positions, m_LightProbes, m_OcclusionProbes);

                for (int i = 0; i < m_Positions.Count; ++i)
                {
                    var shCoefficients = new SHCoefficients(m_LightProbes[i], m_OcclusionProbes[i]);
                    chunkSH[i] = new BuiltinMaterialPropertyUnity_SHCoefficients() {Value = shCoefficients};
                }
            }
            Profiler.EndSample();
        }

        [BurstCompile]
        struct UpdateSHValuesJob : IJobChunk
        {
            public SHCoefficients Coefficients;
            public ComponentTypeHandle<BuiltinMaterialPropertyUnity_SHCoefficients> SHType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var chunkSH = chunk.GetNativeArray(ref SHType);

                for (var i = 0; i < chunkSH.Length; i++)
                {
                    chunkSH[i] = new BuiltinMaterialPropertyUnity_SHCoefficients {Value = Coefficients};
                }
            }
        }
    }
}
