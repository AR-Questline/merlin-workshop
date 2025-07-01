using System;
using System.Reflection;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.SerializableTypeReference;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public static class MaterialOverrideUtils {
        public static void AddMaterialOverrides(in MaterialsOverridePack overridePack, in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity, ref EntityCommandBuffer ecb) {
            var rendererManager = DrakeRendererManager.Instance;
            var loadingManager = rendererManager.LoadingManager;

            loadingManager.TryGetLoadedMaterial(drakeMeshMaterialComponent.materialIndex, out var runtimeKey, out _);

            var appliedAny = false;
            if (overridePack.materialKeys.IsValid) {
                for (var i = 0u; i < overridePack.materialKeys.Length; i++) {
                    if (overridePack.materialKeys[i].Equals(runtimeKey)) {
                        overridePack.overrideDatas[i].AddComponent(entity, ref ecb);
                        appliedAny = true;
                    }
                }
            }
            if (!appliedAny && !overridePack.defaultData.IsValid) {
                for (var i = 0u; i < overridePack.defaultData.Length; i++) {
                    overridePack.defaultData[i].AddComponent(entity, ref ecb);
                }
            }
        }

        public static void ApplyMaterialOverrides(LinkedEntitiesAccess entitiesAccess, in MaterialsOverridePack overridePack) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var entities = entitiesAccess.LinkedEntities;

            var rendererManager = DrakeRendererManager.Instance;
            var loadingManager = rendererManager.LoadingManager;

            var ecb = new EntityCommandBuffer(ARAlloc.Temp);

            foreach (var entity in entities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                loadingManager.TryGetLoadedMaterial(meshMaterial.materialIndex, out var runtimeKey, out _);

                var appliedAny = false;
                if (overridePack.materialKeys.IsValid) {
                    for (var i = 0u; i < overridePack.materialKeys.Length; i++) {
                        if (overridePack.materialKeys[i].Equals(runtimeKey)) {
                            ApplyMaterialOverride(ref entityManager, entity, overridePack.overrideDatas[i], ref ecb);
                            appliedAny = true;
                        }
                    }
                }
                if (!appliedAny && !overridePack.defaultData.IsValid) {
                    for (var i = 0u; i < overridePack.defaultData.Length; i++) {
                        ApplyMaterialOverride(ref entityManager, entity, overridePack.defaultData[i], ref ecb);
                    }
                }
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        public static void ApplyMaterialOverrides(LinkedEntitiesAccess entitiesAccess, in MaterialOverrideData overrideData) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var entities = entitiesAccess.LinkedEntities;

            var ecb = new EntityCommandBuffer(ARAlloc.Temp);

            foreach (var entity in entities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }

                ApplyMaterialOverride(ref entityManager, entity, overrideData, ref ecb);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        public static void ApplyMaterialOverride(ref EntityManager entityManager, Entity entity, in MaterialOverrideData overrideData, ref EntityCommandBuffer ecb) {
            if (entityManager.HasComponent(entity, overrideData.ComponentType)) {
                overrideData.SetComponent(entity, ref ecb);
            } else {
                overrideData.AddComponent(entity, ref ecb);
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static void RemoveMaterialOverrides(LinkedEntitiesAccess entitiesAccess, in MaterialOverrideData overrideData) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var entities = entitiesAccess.LinkedEntities;

            var ecb = new EntityCommandBuffer(ARAlloc.Temp);

            foreach (var entity in entities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                overrideData.RemoveComponent(entity, ref ecb);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        public static void RemoveMaterialOverrides(LinkedEntitiesAccess entitiesAccess, Type componentType) {
            RemoveMaterialOverrides(entitiesAccess, ComponentType.ReadWrite(componentType));
        }

        public static void RemoveMaterialOverrides(LinkedEntitiesAccess entitiesAccess, in ComponentType componentType) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var entities = entitiesAccess.LinkedEntities;

            var ecb = new EntityCommandBuffer(ARAlloc.Temp);

            foreach (var entity in entities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                ecb.RemoveComponent(entity, componentType);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        public static int GetPropertyID(SerializableTypeReference serializedType) {
            var materialProperty = serializedType.Type.GetCustomAttribute<MaterialPropertyAttribute>();
            return Shader.PropertyToID(materialProperty.Name);
        }
    }
}
