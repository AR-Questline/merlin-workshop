using System;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Utility.SerializableTypeReference;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public static class MaterialOverrideUtils {
        public static void ApplyMaterialOverrides(LinkedEntitiesAccess entitiesAccess,
            in MaterialsOverridePack overridePack) {
            throw new NotImplementedException();
        }

        public static void ApplyMaterialOverrides(LinkedEntitiesAccess entitiesAccess,
            in MaterialOverrideData overrideData) {
            throw new NotImplementedException();
        }

        public static void ApplyMaterialOverride(ref EntityManager entityManager, Entity entity,
            in MaterialOverrideData overrideData, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }

        public static void RemoveMaterialOverrides(LinkedEntitiesAccess entitiesAccess,
            in MaterialOverrideData overrideData) {
            throw new NotImplementedException();
        }

        public static void RemoveMaterialOverrides(LinkedEntitiesAccess entitiesAccess, Type componentType) {
            throw new NotImplementedException();
        }

        public static int GetPropertyID(SerializableTypeReference serializedType) {
            throw new NotImplementedException();
        }
    }
}