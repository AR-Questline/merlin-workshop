using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Systems;
using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererDisableActiveSystem : SystemBase {
        EntityQuery _query;
        ComponentTypeSet _toAddComponents;

        protected override void OnCreate() {
            _query = GetEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(DrakeMeshMaterialComponent), typeof(MaterialMeshInfo) },
                Absent = new ComponentType[] {
                    typeof(DrakeRendererLoadingTag), typeof(DrakeRendererLoadRequestTag),
                    typeof(DrakeRendererUnloadRequestTag), typeof(DrakeRendererManualTag)
                },
            });
            _toAddComponents = new(ComponentType.ReadWrite<DrakeRendererUnloadRequestTag>(), ComponentType.ReadWrite<DrakeRendererManualTag>());
            Enabled = false;
        }

        protected override void OnUpdate() {
            EntityManager.AddComponent(_query, _toAddComponents);
        }
    }

    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererEnableActiveSystem : SystemBase {
        EntityQuery _query;
        ComponentTypeSet _toAddComponents;

        protected override void OnCreate() {
            _query = GetEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(DrakeMeshMaterialComponent) },
                Absent = new ComponentType[] {
                    typeof(MaterialMeshInfo), typeof(DrakeRendererLoadingTag), typeof(DrakeRendererUnloadRequestTag),
                    typeof(DrakeRendererLoadRequestTag), typeof(DrakeRendererManualTag),
                },
            });
            _toAddComponents = new(ComponentType.ReadWrite<DrakeRendererLoadRequestTag>(), ComponentType.ReadWrite<DrakeRendererManualTag>());
            Enabled = false;
        }

        protected override void OnUpdate() {
            EntityManager.AddComponent(_query, _toAddComponents);
        }
    }

    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererRemoveManualOverrideSystem : SystemBase {
        EntityQuery _query;

        protected override void OnCreate() {
            _query = GetEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(DrakeRendererManualTag) },
            });
            Enabled = false;
        }

        protected override void OnUpdate() {
            EntityManager.RemoveComponent<DrakeRendererManualTag>(_query);
        }
    }
}
