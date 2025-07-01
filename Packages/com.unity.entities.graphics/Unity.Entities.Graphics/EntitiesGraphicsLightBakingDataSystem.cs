using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.Rendering
{
    /// <summary>
    /// An unmanaged component that stores light baking data.
    /// </summary>
    /// <remarks>
    /// Entities Graphics uses this component to store light baking data at conversion time to restore
    /// at run time. This is because this doesn't happen automatically with hybrid entities.
    /// </remarks>
    public struct LightBakingOutputData : IComponentData
    {
        /// <summary>
        /// The output of light baking on the entity.
        /// </summary>
        public LightBakingOutput Value;
    }

    /// <summary>
    /// Represents a light baking system that assigns a LightBakingOutput to the bakingOutput of the Light component.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class HybridLightBakingDataSystem : SystemBase
    {
        private EntityQuery m_LightBakingQuery;

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        protected override void OnCreate()
        {
            m_LightBakingQuery = SystemAPI.QueryBuilder()
                .WithAll<LightBakingOutputData, Light>()
                .Build();
            m_LightBakingQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Light>());
        }

        /// <summary>
        /// Called when this system is updated.
        /// </summary>
        protected override void OnUpdate()
        {
            var entities = m_LightBakingQuery.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                var bakingOutput = EntityManager.GetComponentData<LightBakingOutputData>(e);
                var light = EntityManager.GetComponentObject<Light>(e);

                if (light != null)
                    light.bakingOutput = bakingOutput.Value;
            }
        }
    }
}
