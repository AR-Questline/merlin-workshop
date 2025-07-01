using Awaken.ECS.Mipmaps.Components;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Awaken.ECS.Mipmaps.Systems {
    [CreateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class MipmapsMissingFactorSystem : SystemBase {
        EntityQuery _missingFactorQuery;

        protected override void OnCreate() {
            _missingFactorQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<RenderMeshArray>(),
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<WorldRenderBounds>(),
                },
                Absent = new[] {
                    ComponentType.ReadOnly<MipmapsFactorComponent>(),
                    ComponentType.ReadOnly<MipmapsTransformFactorComponent>(),
                },
            });
        }

        protected override void OnUpdate() {
            EntityManager.AddComponent<MipmapsTransformFactorComponent>(_missingFactorQuery);
        }
    }
}
