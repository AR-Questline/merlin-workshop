using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.EntitySceneOptimizations)]
    [UpdateAfter(typeof(LODRequirementsUpdateSystem))]
    partial class FreezeStaticLODObjects : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var group = GetEntityQuery(
                new EntityQueryDesc
                {
                    Any = new ComponentType[] { typeof(MeshLODGroupComponent), typeof(MeshLODComponent), typeof(LODGroupWorldReferencePoint) },
                    All = new ComponentType[] { typeof(Static) }
                });

            EntityManager.RemoveComponent(group, new ComponentTypeSet(typeof(MeshLODGroupComponent), typeof(MeshLODComponent), typeof(LODGroupWorldReferencePoint)));
        }
    }
}
