using System;
using Unity.Collections;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererEntitiesManager {
        public NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype> EntityArchetypes =>
            throw new NotImplementedException();

        public DrakeRendererEntitiesManager(EntityManager entityManager) {
            throw new NotImplementedException();
        }

        public EntityArchetype GetLodGroupArchetype() {
            throw new NotImplementedException();
        }

        public EntityArchetype GetRendererArchetype(DrakeRendererArchetypeKey archetypeKey) {
            throw new NotImplementedException();
        }
    }
}