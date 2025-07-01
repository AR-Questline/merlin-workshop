using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.ECS.Utils;
using Awaken.Utility.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererEntitiesManager {
        readonly EntityArchetype _dynamicLodGroupArchetype;
        NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype> _entityArchetypes;

        public NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype> EntityArchetypes => _entityArchetypes;

        public DrakeRendererEntitiesManager(EntityManager entityManager) {
            var archetypeKeys = DrakeRendererArchetypeKey.All;
            _entityArchetypes = new NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype>(archetypeKeys.Length, Allocator.Domain);
            foreach (var archetypeKey in archetypeKeys) {
                _entityArchetypes.Add(archetypeKey, CreateArchetype(archetypeKey, entityManager));
            }

            var baseComponents = new UnsafeList<ComponentType>(6, ARAlloc.Temp) {
                ComponentType.ReadWrite<MeshLODGroupComponent>(),
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<LODGroupWorldReferencePoint>(),
                ComponentType.ReadWrite<LinkedTransformComponent>(),

                ComponentType.ReadWrite<SystemRelatedLifeTime<DrakeRendererManager>.IdComponent>(),
            };
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool("showEntities", false)) {
                baseComponents.Add(ComponentType.ReadWrite<EntityGuid>());
            }
#endif


            _dynamicLodGroupArchetype = entityManager.CreateArchetype(baseComponents.AsNativeArray());
            baseComponents.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetype GetLodGroupArchetype() {
            return _dynamicLodGroupArchetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetype GetRendererArchetype(DrakeRendererArchetypeKey archetypeKey) {
            return _entityArchetypes[archetypeKey];
        }

        // From Unity.Rendering.RenderMeshUtility.EntitiesGraphicsComponentTypes
        EntityArchetype CreateArchetype(DrakeRendererArchetypeKey archetypeKey, EntityManager entityManager) {
            var components = new UnsafeList<ComponentType>(24, ARAlloc.Temp) {
                ComponentType.ReadWrite<WorldRenderBounds>(),
                ComponentType.ReadWrite<DrakeMeshMaterialComponent>(),
                ComponentType.ReadWrite<PerInstanceCullingTag>(),
                ComponentType.ReadWrite<WorldToLocal_Tag>(),
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<MipmapsFactorComponent>(),

                ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),

                ComponentType.ReadWrite<RenderFilterSettings>(),
                ComponentType.ReadWrite<SystemRelatedLifeTime<DrakeRendererManager>.IdComponent>(),
                ComponentType.ReadWrite<ShadowsProcessedTag>(),
            };

            if (archetypeKey.isStatic) {
                components.Add(ComponentType.ReadWrite<Static>());
            } else {
                components.Add(ComponentType.ReadWrite<LinkedTransformComponent>());
                components.Add(ComponentType.ReadWrite<RenderBounds>());
                if (archetypeKey.inMotionPass) {
                    components.Add(ComponentType.ReadWrite<BuiltinMaterialPropertyUnity_MatrixPreviousM>());
                }
                if (archetypeKey.hasLocalToWorldOffset) {
                    components.Add(ComponentType.ReadWrite<LinkedTransformLocalToWorldOffsetComponent>());
                }
            }

            if (archetypeKey.hasLodGroup) {
                components.Add(ComponentType.ReadWrite<DrakeRendererVisibleRangeComponent>());
                components.Add(ComponentType.ReadWrite<LODRange>());
                components.Add(ComponentType.ReadWrite<LODWorldReferencePoint>());
                if (!archetypeKey.isStatic) {
                    components.Add(ComponentType.ReadWrite<MeshLODComponent>());
                }
            } else {
                components.Add(ComponentType.ReadWrite<DrakeRendererLoadRequestTag>());
            }

            if (archetypeKey.isTransparent) {
                components.Add(ComponentType.ReadWrite<DepthSorted_Tag>());
            }
            if (archetypeKey.lightProbeUsage == LightProbeUsage.BlendProbes) {
                components.Add(ComponentType.ReadWrite<BlendProbeTag>());
            } else if (archetypeKey.lightProbeUsage == LightProbeUsage.CustomProvided) {
                components.Add(ComponentType.ReadWrite<CustomProbeTag>());
            }
            if (archetypeKey.hasShadowsOverriden) {
                components.Add(ComponentType.ReadWrite<ShadowsChangedTag>());
            }
#if UNITY_EDITOR
#if DEBUG
            components.Add(ComponentType.ReadWrite<CullingDistancePreviewComponent>());
#endif
            if (!UnityEditor.EditorPrefs.GetBool("showEntities", false)) {
                components.Add(ComponentType.ReadWrite<EntityGuid>());
            }

            components.Add(ComponentType.ReadWrite<EditorRenderData>());
#endif

            var archetype = entityManager.CreateArchetype(components.AsNativeArray());
            components.Dispose();
            return archetype;
        }
    }
}
