using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.EntitySceneOptimizations)]
    partial class FrozenStaticRendererSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var group = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(SceneSection), typeof(RenderMesh), typeof(LocalToWorld), typeof(Static) },
                    None = new ComponentType[] { typeof(FrozenRenderSceneTag) }
                });

            EntityManager.GetAllUniqueSharedComponents<SceneSection>(out var sections, Allocator.Temp);

            // @TODO: Perform full validation that all Low LOD levels are in section 0
            int hasStreamedLOD = 0;
            foreach (var section in sections)
            {
                group.SetSharedComponentFilterManaged(section);
                if (section.Section != 0)
                    hasStreamedLOD = 1;
            }

            foreach (var section in sections)
            {
                group.SetSharedComponentFilterManaged(section);
                EntityManager.AddSharedComponentManaged(group, new FrozenRenderSceneTag { SceneGUID = section.SceneGUID, SectionIndex = section.Section, HasStreamedLOD = hasStreamedLOD});
            }

            group.ResetFilter();
        }
    }
}
