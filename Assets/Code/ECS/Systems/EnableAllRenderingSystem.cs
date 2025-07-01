using Awaken.ECS.Components;
using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.Systems {
    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class EnableAllRenderingSystem : SystemBase {
        EntityQuery _query;
        ComponentTypeSet _disableRenderingSet;
        
        protected override void OnCreate() {
            var componentTypes = new ComponentType[] { typeof(DisableRendering), typeof(ARDisabledRenderingTag) };
            _disableRenderingSet = new(componentTypes);
            _query = GetEntityQuery(new EntityQueryDesc {
                All = componentTypes,
            });
            Enabled = false;
        }

        protected override void OnUpdate() {
            EntityManager.RemoveComponent(_query, _disableRenderingSet);
        }
    }
}
