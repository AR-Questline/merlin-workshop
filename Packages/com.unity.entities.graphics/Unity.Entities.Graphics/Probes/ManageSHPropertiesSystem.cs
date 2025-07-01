// #define DISABLE_HYBRID_LIGHT_PROBES

using System;
using System.Linq;
using Unity.Entities;
using UnityEngine;

#if !DISABLE_HYBRID_LIGHT_PROBES
namespace Unity.Rendering
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(StructuralChangePresentationSystemGroup))]
    partial class ManageSHPropertiesSystem : SystemBase
    {
        // Match entities with CustomProbeTag, but without the SH component
        EntityQuery m_MissingSHQueryCustom;
        // Match entities with BlendProbeTag, but without the SH component
        EntityQuery m_MissingSHQueryBlend;

        // Matches entities with the SH component, but neither CustomProbeTag or BlendProbeTag
        EntityQuery m_MissingProbeTagQuery;
        // Matches entities with SH components and BlendProbeTag
        EntityQuery m_RemoveSHFromBlendProbeTagQuery;

        ComponentType[] m_SHComponentType;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_SHComponentType = new[]
            {
                ComponentType.ReadOnly<BuiltinMaterialPropertyUnity_SHCoefficients>(),
            };

            m_MissingSHQueryCustom = GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<CustomProbeTag>()
                },
                None = m_SHComponentType,
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            m_MissingSHQueryBlend = GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<BlendProbeTag>(),
                },
                None = m_SHComponentType,
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            m_MissingProbeTagQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = m_SHComponentType,
                None = new[]
                {
                    ComponentType.ReadOnly<BlendProbeTag>(),
                    ComponentType.ReadOnly<CustomProbeTag>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            m_RemoveSHFromBlendProbeTagQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = m_SHComponentType,
                All = new []{ ComponentType.ReadOnly<BlendProbeTag>(), },
            });
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // If there is a valid light probe grid, BlendProbeTag entities should have SH components
            // If there is no valid light probe grid, BlendProbeTag entities will not have SH components
            // and behave as if they had AmbientProbeTag instead (read from global probe).
            bool validGrid = LightProbeUpdateSystem.IsValidLightProbeGrid();

            // CustomProbeTag entities should always have SH components
            EntityManager.AddComponent(m_MissingSHQueryCustom, m_SHComponentType[0]);

            // BlendProbeTag entities have SH components if and only if there's a valid light probe grid
            if (validGrid)
                EntityManager.AddComponent(m_MissingSHQueryBlend, m_SHComponentType[0]);
            else
                EntityManager.RemoveComponent(m_RemoveSHFromBlendProbeTagQuery, m_SHComponentType[0]);

            // AmbientProbeTag entities never have SH components
            EntityManager.RemoveComponent(m_MissingProbeTagQuery, m_SHComponentType[0]);
        }
    }
}
#endif
