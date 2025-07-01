using Awaken.ECS.Components;
using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.Systems {
    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DisableAllRenderingSystem : SystemBase {
        EntityQuery _query;
        ComponentTypeSet _disableRenderingSet;
        
        protected override void OnCreate() {
            var componentTypes = new ComponentType[] { typeof(DisableRendering), typeof(ARDisabledRenderingTag) };
            _disableRenderingSet = new(componentTypes);
            _query = GetEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(MaterialMeshInfo) },
                Absent = componentTypes,
            });
            Enabled = false;
        }

        protected override void OnUpdate() {
            EntityManager.AddComponent(_query, _disableRenderingSet);
        }
    }
}
