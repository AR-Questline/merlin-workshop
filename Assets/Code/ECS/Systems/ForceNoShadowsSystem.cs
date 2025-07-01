using Awaken.Utility.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using UnityEngine.Rendering;

namespace Awaken.ECS.Systems {
    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class ForceNoShadowsSystem : SystemBase {
        EntityQuery _query;

        protected override void OnCreate() {
            _query = GetEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(RenderFilterSettings) },
            });
            Enabled = false;
        }

        protected override void OnUpdate() {
            var entities = _query.ToEntityArray(ARAlloc.Temp);
            foreach (var entity in entities) {
                var filterSettings = EntityManager.GetSharedComponent<RenderFilterSettings>(entity);
                if (filterSettings.ShadowCastingMode != ShadowCastingMode.Off) {
                    filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
                    EntityManager.SetSharedComponent(entity, filterSettings);
                }
            }
            entities.Dispose();
        }
    }
}