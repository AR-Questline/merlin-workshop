using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.ECS.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Files;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeMergedRenderers : MonoBehaviourWithInitAfterLoaded {
        [SerializeField] SerializableGuid dataGuid;

        public override void Init() {
            var fileName = DrakeMergedRenderersLoading.Instance.GetFilePath(dataGuid);
            var fileContent = FileRead.ToNewBuffer<byte>(fileName, ARAlloc.Temp);
            using var fileScope = new UnsafeArray<byte>.ScopeGuard(ref fileContent);
            var fileReader = new BufferStreamReader(fileContent);

            var lodGroupsCount = fileReader.Read<uint>();
            var lodData = fileReader.ReadSpan<LodGroup>(lodGroupsCount);

            var meshesCount = fileReader.Read<uint>();
            var meshData = fileReader.ReadSpan<MeshData>(meshesCount);

            var materialsCount = fileReader.Read<uint>();
            var materialData = fileReader.ReadSpan<SerializableGuid>(materialsCount);

            var definitionsCount = fileReader.Read<uint>();
            var rendererDefs = fileReader.ReadSpan<SerializableRendererDefinition>(definitionsCount);

            var rendererInstancesCount = fileReader.Read<uint>();
            var renderers = fileReader.ReadSpan<RendererInstance>(rendererInstancesCount);

            var prototypeAdditionalSet = new ComponentTypeSet(ComponentType.ReadWrite<UVDistributionMetricComponent>());

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var drakeArchetypeByKey = DrakeRendererManager.Instance.EntitiesManager.EntityArchetypes;
            var inflatedArchetypeByKey = new NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype>(drakeArchetypeByKey.Count, ARAlloc.TempJob);
            foreach (var archetypesData in drakeArchetypeByKey) {
                if (archetypesData.Key.isStatic == false || archetypesData.Key.hasLocalToWorldOffset) {
                    continue;
                }
                var archetypeComponents = archetypesData.Value.GetComponentTypesWith(prototypeAdditionalSet, ARAlloc.Temp);
                var archetype = entityManager.CreateArchetype(archetypeComponents);
                inflatedArchetypeByKey.Add(archetypesData.Key, archetype);
                archetypeComponents.Dispose();
            }

            var loadingManager = DrakeRendererManager.Instance.LoadingManager;
            var componentsManager = DrakeRendererManager.Instance.ComponentsManager;

            var meshCache = new NativeHashMap<FixedString128Bytes, ushort>((int)meshData.Length, ARAlloc.Temp);
            var materialCache = new NativeHashMap<Guid, ushort>((int)meshData.Length*3, ARAlloc.Temp);
            var definitions = new UnsafeArray<RuntimeRendererDefinition>(definitionsCount, ARAlloc.Temp);

            for (var definitionIndex = 0u; definitionIndex < definitionsCount; ++definitionIndex) {
                var definition = rendererDefs[definitionIndex];

                var materialsRange = definition.materialsRange;
                var meshMaterials = new UnsafeArray<DrakeMeshMaterialComponent>(materialsRange.length, ARAlloc.Temp);
                var meshLinkIndex = definition.meshIndex;
                var meshKey = meshData[meshLinkIndex].meshKey;
                var meshIndex = GetMeshIndex(meshKey);

                for (var materialIterator = 0u; materialIterator < materialsRange.length; ++materialIterator) {
                    var materialKey = materialData[materialIterator + materialsRange.start];
                    var materialIndex = GetMaterialIndex(materialKey);
                    var meshMaterial = new DrakeMeshMaterialComponent(meshIndex, materialIndex, (sbyte)materialIterator);
                    componentsManager.Register(meshMaterial);
                    meshMaterials[materialIterator] = meshMaterial;
                }
                definitions[definitionIndex] = new RuntimeRendererDefinition {
                    renderFilterSettings = definition.renderFilterSettings.ToRenderFilterSettings(),
                    lodMask = definition.lodMask,
                    meshIndex = meshLinkIndex,
                    lightProbeUsage = definition.lightProbeUsage,
                    transparentMask = definition.transparentMask,
                    meshMaterials = meshMaterials,
                };
            }
            meshCache.Dispose();
            materialCache.Dispose();

            var scene = gameObject.scene;
            var idComponent = DrakeRendererManager.GetSceneIdComponent(scene);

            new SpawnJob {
                entityManager = entityManager,
                shadowVolumeData = SmallObjectsShadows.LoadData(),
                archetypes = inflatedArchetypeByKey,
                idComponent = idComponent,
#if UNITY_EDITOR
                editorRenderData = new EditorRenderData { SceneCullingMask = gameObject.sceneCullingMask },
#endif

                lodGroupsData = lodData,
                meshesData = meshData,
                rendererDefinitions = definitions,
                rendererInstances = renderers,
            }.Run();

            for (var i = 0u; i < definitionsCount; ++i) {
                definitions[i].meshMaterials.Dispose();
            }
            definitions.Dispose();
            inflatedArchetypeByKey.Dispose();

            Destroy(gameObject);

            ushort GetMeshIndex(FixedString128Bytes key) {
                if (meshCache.TryGetValue(key, out var index)) {
                    return index;
                }
                index = loadingManager.GetMeshIndex(key.ToString());
                meshCache.Add(key, index);
                return index;
            }

            ushort GetMaterialIndex(SerializableGuid key) {
                if (materialCache.TryGetValue(key, out var index)) {
                    return index;
                }
                index = loadingManager.GetMaterialIndex(key.Guid.ToString("N"));
                materialCache.Add(key, index);
                return index;
            }
        }

        [BurstCompile]
        struct SpawnJob : IJob {
            public EntityManager entityManager;
            public UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum>.Span shadowVolumeData;
            public NativeHashMap<DrakeRendererArchetypeKey, EntityArchetype> archetypes;
            public SystemRelatedLifeTime<DrakeRendererManager>.IdComponent idComponent;
#if UNITY_EDITOR
            public EditorRenderData editorRenderData;
#endif

            // Per lod group
            public UnsafeArray<LodGroup>.Span lodGroupsData;
            // Per mesh
            public UnsafeArray<MeshData>.Span meshesData;
            // Per definition
            public UnsafeArray<RuntimeRendererDefinition>.Span rendererDefinitions;
            // Per renderer
            public UnsafeArray<RendererInstance>.Span rendererInstances;

            public void Execute() {
                for (var i = 0u; i < rendererInstances.Length; i++) {
                    ProcessRenderer(i);
                }
            }

            void ProcessRenderer(uint rendererIndex) {
                var rendererInstance = rendererInstances[rendererIndex];
                ref readonly var definition = ref rendererDefinitions[rendererInstance.definitionIndex];
                ref readonly var meshData = ref meshesData[definition.meshIndex];

                var lodGroupIndex = rendererInstance.lodGroupIndex;
                var hasLodGroup = lodGroupIndex != 0;
                ref readonly var lodGroupData = ref lodGroupsData[lodGroupIndex];

                var localToWorld = new LocalToWorld {
                    Value = rendererInstance.localToWorld.expandOrhonormal(),
                };

                var aabb = meshData.aabb;

                var worldRenderBounds = new WorldRenderBounds {
                    Value = AABB.Transform(localToWorld.Value, aabb),
                };

                var lodMask = definition.lodMask;
                LODRange lodRange = default;
                var changedShadows = false;
                var filterSettings = definition.renderFilterSettings;

                if (hasLodGroup) {
                    lodRange = lodGroupData.CreateLODRange(lodMask);
                    changedShadows = ProcessFilterSettings(ref filterSettings, lodRange, worldRenderBounds.Value);
                }

                var subRenderersCount = definition.meshMaterials.Length;
                var entities = new NativeArray<Entity>((int)subRenderersCount, ARAlloc.InJobTemp);

                var completion = unchecked((uint)~((1<<((int)subRenderersCount))-1));
                var singleTransparency = (definition.transparentMask == 0) | ((definition.transparentMask | completion) == uint.MaxValue);
                var archetypeKey = new DrakeRendererArchetypeKey {
                    isStatic = true,
                    hasLodGroup = hasLodGroup,
                    inMotionPass = filterSettings.MotionMode != MotionVectorGenerationMode.Camera,
                    hasShadowsOverriden = changedShadows,
                    lightProbeUsage = (LightProbeUsage)(int)definition.lightProbeUsage,
                };

                if (singleTransparency) {
                    archetypeKey.isTransparent = (definition.transparentMask & 1) == 1;
                    var archetype = archetypes[archetypeKey];
                    entityManager.CreateEntity(archetype, entities);
                } else {
                    for (var i = 0; i < entities.Length; i++) {
                        archetypeKey.isTransparent = ((definition.transparentMask & (1 << i)) >> i) == 1;

                        var archetype = archetypes[archetypeKey];
                        entities[i] = entityManager.CreateEntity(archetype);
                    }
                }

                entityManager.SetSharedComponent(entities, idComponent);
                entityManager.SetSharedComponent(entities, filterSettings);
#if UNITY_EDITOR
                entityManager.SetSharedComponent(entities, editorRenderData);
#endif

                if (hasLodGroup) {
                    var visibleRange = DrakeMeshRenderer.PrepareRanges(lodGroupData.distances0, lodGroupData.distances1, lodMask);
                    var drakeRendererVisibleRangeComponent = new DrakeRendererVisibleRangeComponent(visibleRange);

                    for (var i = 0u; i < definition.meshMaterials.Length; i++) {
                        var entity = entities[(int)i];
                        entityManager.SetComponentData(entity, localToWorld);
                        entityManager.SetComponentData(entity, worldRenderBounds);
                        entityManager.SetComponentData(entity, definition.meshMaterials[i]);
                        entityManager.SetComponentData(entity, new UVDistributionMetricComponent { value = meshData.uvDistribution });
                        entityManager.SetComponentData(entity, lodRange);
                        entityManager.SetComponentData(entity, new LODWorldReferencePoint { Value = lodGroupData.worldReferencePoint });
                        entityManager.SetComponentData(entity, drakeRendererVisibleRangeComponent);
                    }
                } else {
                    for (var i = 0u; i < definition.meshMaterials.Length; i++) {
                        var entity = entities[(int)i];
                        entityManager.SetComponentData(entity, localToWorld);
                        entityManager.SetComponentData(entity, worldRenderBounds);
                        entityManager.SetComponentData(entity, definition.meshMaterials[i]);
                        entityManager.SetComponentData(entity, new UVDistributionMetricComponent { value = meshData.uvDistribution });
                    }
                }

                entities.Dispose();
            }

            bool ProcessFilterSettings(ref RenderFilterSettings filterSettings, in LODRange lodRange, in AABB worldAABB) {
                if (filterSettings.ShadowCastingMode == ShadowCastingMode.On) {
                    if (SmallObjectsShadows.ShouldDisableShadows(lodRange, worldAABB, shadowVolumeData)) {
                        filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
                        return true;
                    }
                }
                return false;
            }
        }

        // Serializable & Runtime
        public struct LodGroup {
            public float4 distances0;
            public float4 distances1;
            public float3 worldReferencePoint;

            public readonly LODRange CreateLODRange(int lodMask) {
                float minDist = float.MaxValue;
                float maxDist = 0.0F;

                if ((lodMask & 1 << 0) == 1 << 0) {
                    minDist = 0.0f;
                    maxDist = math.max(maxDist, distances0.x);
                }
                if ((lodMask & 1 << 1) == 1 << 1) {
                    minDist = math.min(minDist, distances0.x);
                    maxDist = math.max(maxDist, distances0.y);
                }
                if ((lodMask & 1 << 2) == 1 << 2) {
                    minDist = math.min(minDist, distances0.y);
                    maxDist = math.max(maxDist, distances0.z);
                }
                if ((lodMask & 1 << 3) == 1 << 3) {
                    minDist = math.min(minDist, distances0.z);
                    maxDist = math.max(maxDist, distances0.w);
                }
                if ((lodMask & 1 << 4) == 1 << 4) {
                    minDist = math.min(minDist, distances0.w);
                    maxDist = math.max(maxDist, distances1.x);
                }
                if ((lodMask & 1 << 5) == 1 << 5) {
                    minDist = math.min(minDist, distances1.x);
                    maxDist = math.max(maxDist, distances1.y);
                }
                if ((lodMask & 1 << 6) == 1 << 6) {
                    minDist = math.min(minDist, distances1.y);
                    maxDist = math.max(maxDist, distances1.z);
                }
                if ((lodMask & 1 << 7) == 1 << 7) {
                    minDist = math.min(minDist, distances1.z);
                    maxDist = math.max(maxDist, distances1.w);
                }

                return new LODRange() {
                    LODMask = lodMask,
                    MinDist = minDist,
                    MaxDist = maxDist,
                };
            }
        }

        // Serializable & Runtime
        public struct MeshData {
            public FixedString128Bytes meshKey;
            public AABB aabb;
            public float uvDistribution;
        }

        // Serializable
        public struct SerializableRendererDefinition {
            public SerializableFilterSettings renderFilterSettings;
            public int lodMask;
            public uint meshIndex;
            public byte lightProbeUsage;
            public byte transparentMask;
            public IndexRange materialsRange;
        }

        // Serializable & Runtime
        public struct RendererInstance {
            public float3x4 localToWorld;
            public uint lodGroupIndex;
            public uint definitionIndex;
        }

        // Authoring
        public struct AuthoringRendererDefinition : IEquatable<AuthoringRendererDefinition> {
            public SerializableFilterSettings renderFilterSettings;
            public int lodMask;
            public uint meshIndex;
            public byte lightProbeUsage;
            public byte transparentMask;
            public SerializableGuid[] materialKeys;

            public bool Equals(AuthoringRendererDefinition other) {
                var simpleEquality = renderFilterSettings.Equals(other.renderFilterSettings) &&
                                     lodMask == other.lodMask &&
                                     meshIndex == other.meshIndex &&
                                     lightProbeUsage == other.lightProbeUsage &&
                                     transparentMask == other.transparentMask &&
                                     materialKeys.Length == other.materialKeys.Length;

                if (!simpleEquality) {
                    return false;
                }

                for (var i = 0; i < materialKeys.Length; ++i) {
                    if (materialKeys[i] != other.materialKeys[i]) {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj) {
                return obj is AuthoringRendererDefinition other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = renderFilterSettings.GetHashCode();
                    hashCode = (hashCode * 397) ^ lodMask;
                    hashCode = (hashCode * 397) ^ (int)meshIndex;
                    hashCode = (hashCode * 397) ^ lightProbeUsage.GetHashCode();
                    hashCode = (hashCode * 397) ^ transparentMask.GetHashCode();
                    hashCode = (hashCode * 397) ^ (materialKeys != null ? materialKeys.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(AuthoringRendererDefinition left, AuthoringRendererDefinition right) {
                return left.Equals(right);
            }

            public static bool operator !=(AuthoringRendererDefinition left, AuthoringRendererDefinition right) {
                return !left.Equals(right);
            }
        }

        // Runtime
        struct RuntimeRendererDefinition {
            public RenderFilterSettings renderFilterSettings;
            public int lodMask;
            public uint meshIndex;
            public byte lightProbeUsage;
            public byte transparentMask;
            public UnsafeArray<DrakeMeshMaterialComponent> meshMaterials;
        }

        public struct EditorAccess {
            public static ref SerializableGuid DataGuid(DrakeMergedRenderers target) => ref target.dataGuid;
        }
    }
}