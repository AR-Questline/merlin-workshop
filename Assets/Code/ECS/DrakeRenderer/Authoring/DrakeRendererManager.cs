using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.ECS.Utils;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using QFSW.QC;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Log = Awaken.Utility.Debugging.Log;
using LogType = Awaken.Utility.Debugging.LogType;

[assembly: RegisterGenericComponentType(typeof(SystemRelatedLifeTime<DrakeRendererManager>.IdComponent))]

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererManager : ISystemWithLifetime {
        static readonly Dictionary<int, Scene> SceneRemap = new();

        public static DrakeRendererManager Instance { get; private set; }

        static bool s_toUnityMode;

        readonly DrakeRendererLoadingManager _loadingManager;
        readonly DrakeRendererEntitiesManager _entitiesManager;
        readonly DrakeRendererComponentsManager _componentsManager;
        readonly BeginPresentationEntityCommandBufferSystem _beginPresentationEntityCommandBufferSystem;
        EntityCommandBuffer _ecb;
        int _ecbFrame = -1;
        UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> _shadowVolumeData;

        Entity _dummy;

        public DrakeRendererLoadingManager LoadingManager => _loadingManager;
        public DrakeRendererComponentsManager ComponentsManager => _componentsManager;
        public DrakeRendererEntitiesManager EntitiesManager => _entitiesManager;

        DrakeRendererManager(EntityManager entityManager) {
            var world = entityManager.World;
            _beginPresentationEntityCommandBufferSystem = world.GetExistingSystemManaged<BeginPresentationEntityCommandBufferSystem>();

            _loadingManager = new();
            _entitiesManager = new(entityManager);
            _componentsManager = new(_loadingManager, world.GetExistingSystemManaged<EntitiesGraphicsSystem>());

            _dummy = entityManager.CreateEntity();
        }

        public static void Create() {
            if (Instance != null) {
                Instance.Destroy();
            }

            SystemRelatedLifeTime<DrakeRendererManager>.InitQuery();
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Instance = new(entityManager);
            Instance.Initialize();
        }

        void Initialize() {
            _shadowVolumeData = SmallObjectsShadows.LoadData(ShadowsConfigChanged);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
            }
#endif
        }

        void Destroy() {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= OnSceneClosing;
            }
#endif
            SmallObjectsShadows.ReleaseData(ShadowsConfigChanged);
        }

        void OnSceneClosing(Scene scene, bool removingScene) {
            OnSceneUnloaded(scene);
        }

        void OnSceneUnloaded(Scene scene) {
            var idComponent = new SystemRelatedLifeTime<DrakeRendererManager>.IdComponent(scene.handle);
            SystemRelatedLifeTime<DrakeRendererManager>.DestroyEntities(idComponent);
        }

        public static void InitializationUpdate() {
            Instance.InitializationUpdateInstance();
        }

        void InitializationUpdateInstance() {
            _componentsManager.UpdateLoadings();
        }

        public void Register(DrakeLodGroup drakeLodGroup, Scene scene) {
            ValidateEcb();
            Register(drakeLodGroup, scene, _ecb, _loadingManager, _entitiesManager, _componentsManager, _shadowVolumeData);
        }
        
        public void Register(DrakeLodGroup drakeLodGroup, Scene scene, EntityCommandBuffer ecb, DynamicBuffer<Entity> addedEntitiesFromEcb = default) {
            Register(drakeLodGroup, scene, ecb, _loadingManager, _entitiesManager, _componentsManager, _shadowVolumeData, addedEntitiesFromEcb);
        }
        
        static void Register(DrakeLodGroup drakeLodGroup, Scene scene, EntityCommandBuffer ecb,
            DrakeRendererLoadingManager loadingManager, DrakeRendererEntitiesManager entitiesManager,
            DrakeRendererComponentsManager componentsManager, UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> shadowVolumeData,
            DynamicBuffer<Entity> addedEntitiesFromEcb = default) {
            if (!drakeLodGroup.IsBaked || drakeLodGroup.Renderers.Length == 0) {
                Log.Minor?.Error($"Invalid DrakeLodGroup {drakeLodGroup.name} has no children, group wont be registered.", drakeLodGroup);
#if !UNITY_EDITOR
                drakeLodGroup.ClearRuntime(true);
#endif
                return;
            }

            if (s_toUnityMode) {
                AsLodGroup(drakeLodGroup);
                drakeLodGroup.ClearRuntime(true);
                return;
            }

            var lodGroupModificationSteps = drakeLodGroup.GetComponents<IDrakeLODBakingModificationStep>();
            for (int i = 0; i < lodGroupModificationSteps.Length; i++) {
                try {
                    lodGroupModificationSteps[i].ModifyDrakeLODGroup(drakeLodGroup);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            var idComponent = GetSceneIdComponent(scene);

            var isStatic = drakeLodGroup.IsStatic;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                isStatic = false;
            }
#endif

            Entity entity = default;
            var entityGuid = new EntityGuid(drakeLodGroup.gameObject.GetHashCode(), 1, (uint)idComponent.id, 0);

            if (!isStatic) {
                var archetype = entitiesManager.GetLodGroupArchetype();

                var localToWorld = new LocalToWorld {
                    Value = drakeLodGroup.transform.localToWorldMatrix,
                };

                entity = ecb.CreateEntity(archetype);
                if (addedEntitiesFromEcb.IsCreated) {
                    addedEntitiesFromEcb.Add(entity);
                }
#if UNITY_EDITOR
                var name = drakeLodGroup.name;
                var truncatedName = new FixedString64Bytes();
                truncatedName.CopyFromTruncated(name);
                ecb.SetName(entity, truncatedName);
                if (UnityEditor.EditorPrefs.GetBool("showEntities", false)) {
                    ecb.SetComponent(entity, entityGuid);
                }
#endif
                ecb.SetComponent(entity, localToWorld);
                ecb.SetComponent(entity, drakeLodGroup.MeshLODGroupComponent);
                var transform = drakeLodGroup.transform;
                var linkedTransformComponent = new LinkedTransformComponent(transform);
                ecb.SetComponent(entity, linkedTransformComponent);
                ecb.SetSharedComponent(entity, idComponent);

                if (drakeLodGroup.HasEntitiesAccess) {
                    var linkedEntitiesAccess = LinkedEntitiesAccess.GetOrCreate(drakeLodGroup.gameObject);
                    ecb.AddComponent(entity, new LinkedEntitiesAccessRequest(linkedEntitiesAccess, drakeLodGroup.HasLinkedLifetime));
                }
                if (drakeLodGroup.HasLinkedLifetime) {
                    LinkedEntityLifetime.GetOrCreate(drakeLodGroup.gameObject);
                }
            }

            var bakingSteps = drakeLodGroup.GetComponents<IDrakeLODBakingStep>();
            for (int i = 0; i < bakingSteps.Length; i++) {
                try {
                    bakingSteps[i].AddDrakeEntityComponents(drakeLodGroup, entity, ref ecb);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            foreach (var drakeMeshRenderer in drakeLodGroup.Renderers) {
                if (!drakeMeshRenderer) {
                    Log.Important?.Error($"DrakeLodGroup {drakeLodGroup.name} has invalid renderer", drakeLodGroup);
                    continue;
                }

                if (isStatic) {
                    Register(drakeMeshRenderer, scene, ecb, loadingManager, entitiesManager, componentsManager, shadowVolumeData,
                        default, drakeLodGroup.LodGroupSerializableData, true, addedEntitiesFromEcb
#if UNITY_EDITOR
                        , entityGuid.OriginatingId
#endif
                    );
                } else {
                    Register(drakeMeshRenderer, scene, ecb, loadingManager, entitiesManager, componentsManager, shadowVolumeData,
                        entity, drakeLodGroup.LodGroupSerializableData, false, addedEntitiesFromEcb
#if UNITY_EDITOR
                        , entityGuid.OriginatingId
#endif
                    );
                }
            }

            var lodGroupFinishBakingListeners = drakeLodGroup.GetComponents<IDrakeLODFinishBakingListener>();
            for (int i = 0; i < lodGroupFinishBakingListeners.Length; i++) {
                try {
                    lodGroupFinishBakingListeners[i].OnDrakeLodGroupBakingFinished();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            drakeLodGroup.ClearRuntime(!isStatic);
        }

        public void Register(DrakeMeshRenderer drakeMeshRenderer, Scene scene,
            Entity lodGroupEntity = default, in LodGroupSerializableData lodGroupData = default, bool? staticOverride = null
#if UNITY_EDITOR
            , int? originalId = null
#endif
        ) {
            ValidateEcb();
            Register(drakeMeshRenderer, scene, _ecb, _loadingManager, _entitiesManager, _componentsManager,
                _shadowVolumeData, lodGroupEntity, lodGroupData, staticOverride);
        }

        static void Register(DrakeMeshRenderer drakeMeshRenderer, Scene scene,
            EntityCommandBuffer ecb, DrakeRendererLoadingManager loadingManager,
            DrakeRendererEntitiesManager entitiesManager,
            DrakeRendererComponentsManager componentsManager, in UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> shadowVolumeData,
            Entity lodGroupEntity = default, in LodGroupSerializableData lodGroupData = default, bool? staticOverride = null,
            DynamicBuffer<Entity> addedEntitiesFromEcb = default
#if UNITY_EDITOR
            , int? originalId = null
#endif
        ) {
            if (!drakeMeshRenderer.IsBaked) {
                Log.Important?.Error($"Invalid DrakeMeshRenderer {drakeMeshRenderer.name} is not baked but want register, renderer wont be registered.", drakeMeshRenderer);
#if !UNITY_EDITOR
                drakeMeshRenderer.Clear(true);
#endif
                return;
            }

            drakeMeshRenderer.ResetModifiedBakingAABB();

            var drakeMeshRendererModificationSteps = drakeMeshRenderer.GetComponents<IDrakeMeshRendererBakingModificationStep>();
            for (int i = 0; i < drakeMeshRendererModificationSteps.Length; i++) {
                drakeMeshRendererModificationSteps[i].ModifyDrakeMeshRenderer(drakeMeshRenderer);
            }

            var localToWorld = new LocalToWorld {
                Value = drakeMeshRenderer.LocalToWorld,
            };
            // If not modified, ExpandedBakingAABB will be equal to initial AABB
            var aabb = drakeMeshRenderer.ExpandedBakingAABB;

            var renderBounds = new RenderBounds {
                Value = aabb,
            };
            var worldRenderBounds = new WorldRenderBounds {
                Value = AABB.Transform(localToWorld.Value, aabb),
            };

            var idComponent = GetSceneIdComponent(scene);

            var isStatic = staticOverride ?? drakeMeshRenderer.IsStatic;
            var description = drakeMeshRenderer.RenderMeshDescription(isStatic);
            var filterSettings = description.FilterSettings;
            var changedShadows = ProcessFilterSettings(ref filterSettings, lodGroupData, drakeMeshRenderer.LodMask, worldRenderBounds.Value, shadowVolumeData);
            var drakeRendererVisibleRangeComponent = drakeMeshRenderer.VisibleRange;

            var meshKey = (string)drakeMeshRenderer.MeshReference.RuntimeKey;
            var meshIndex = loadingManager.GetMeshIndex(meshKey);

            var materialReferences = drakeMeshRenderer.MaterialReferences;

            var hasLodGroup = lodGroupEntity != Entity.Null;
            var hasLocalToWorldOffset = drakeMeshRenderer.LocalToWorldOffset.Equals(float4x4.identity) == false;

            var lodComponent = new MeshLODComponent {
                Group = lodGroupEntity,
                LODMask = drakeMeshRenderer.LodMask,
            };

            LinkedTransformComponent linkedTransformComponent = default;
            LinkedTransformLocalToWorldOffsetComponent offsetComponent = default;
            var transform = drakeMeshRenderer.transform;
            if (!isStatic) {
                linkedTransformComponent = new LinkedTransformComponent(transform);
                offsetComponent = new LinkedTransformLocalToWorldOffsetComponent(drakeMeshRenderer.LocalToWorldOffset);
            }

#if UNITY_EDITOR
            var name = drakeMeshRenderer.name;
            var truncatedName = new FixedString64Bytes();
            truncatedName.CopyFromTruncated(name);
            var sceneCullingMask = drakeMeshRenderer.gameObject.sceneCullingMask;
#endif

            LinkedEntitiesAccessRequest linkedEntitiesAccessRequest = default;
            if (drakeMeshRenderer.HasEntitiesAccess) {
                var linkedEntitiesAccess = LinkedEntitiesAccess.GetOrCreate(drakeMeshRenderer.gameObject);
                linkedEntitiesAccessRequest = new LinkedEntitiesAccessRequest(linkedEntitiesAccess, drakeMeshRenderer.HasLinkedLifetime);
            }
            if (drakeMeshRenderer.HasLinkedLifetime) {
                LinkedEntityLifetime.GetOrCreate(drakeMeshRenderer.gameObject);
            }

            var bakingSteps = drakeMeshRenderer.GetComponents<IDrakeMeshRendererBakingStep>();
            for (var i = 0; i < materialReferences.Length; i++) {
                var materialKey = (string)materialReferences[i].RuntimeKey;
                var materialIndex = loadingManager.GetMaterialIndex(materialKey);
                var meshMaterial = new DrakeMeshMaterialComponent(meshIndex, materialIndex, (sbyte)i);

                componentsManager.Register(meshMaterial);

                var archetypeKey = drakeMeshRenderer.ArchetypeKeys[i];
                archetypeKey.isStatic = staticOverride ?? archetypeKey.isStatic;
                archetypeKey.hasShadowsOverriden = changedShadows;
                archetypeKey.hasLodGroup = hasLodGroup | lodGroupData.isValid;
                archetypeKey.hasLocalToWorldOffset = hasLocalToWorldOffset;

                var archetype = entitiesManager.GetRendererArchetype(archetypeKey);
                var entity = ecb.CreateEntity(archetype);
                if (addedEntitiesFromEcb.IsCreated) {
                    addedEntitiesFromEcb.Add(entity);
                }
#if UNITY_EDITOR
                ecb.SetName(entity, truncatedName);
                if (!UnityEditor.EditorPrefs.GetBool("showEntities", false)) {
                    var drakeGameObjectId = originalId ?? drakeMeshRenderer.gameObject.GetHashCode();
                    var entityGuid = new EntityGuid(drakeGameObjectId, 1, (uint)idComponent.id, (uint)i + 1);
                    ecb.SetComponent(entity, entityGuid);
                }

                ecb.SetSharedComponent(entity, new EditorRenderData { SceneCullingMask = sceneCullingMask });
#endif

                ecb.SetComponent(entity, localToWorld);
                ecb.SetComponent(entity, worldRenderBounds);
                ecb.SetComponent(entity, meshMaterial);
                if (hasLodGroup) {
                    ecb.SetComponent(entity, lodComponent);
                    ecb.SetComponent(entity, drakeRendererVisibleRangeComponent);
                } else if (lodGroupData.isValid) {
                    var lodRange = lodGroupData.CreateLODRange(drakeMeshRenderer.LodMask);
                    ecb.SetComponent(entity, lodRange);
                    ecb.SetComponent(entity, lodGroupData.ToWorldReferencePoint());
                    ecb.SetComponent(entity, drakeRendererVisibleRangeComponent);
                }

                if (!isStatic) {
                    ecb.SetComponent(entity, linkedTransformComponent);
                    ecb.SetComponent(entity, renderBounds);
                    if (hasLocalToWorldOffset) {
                        ecb.SetComponent(entity, offsetComponent);
                    }
                }

                ecb.SetSharedComponent(entity, filterSettings);
                ecb.SetSharedComponent(entity, idComponent);

                if (linkedEntitiesAccessRequest != default) {
                    ecb.AddComponent(entity, linkedEntitiesAccessRequest);
                }

                for (int stepIndex = 0; stepIndex < bakingSteps.Length; stepIndex++) {
                    try {
                        bakingSteps[stepIndex].AddComponentsDrakeRendererEntity(drakeMeshRenderer, lodGroupEntity, in lodGroupData, in meshMaterial, entity, ref ecb);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }

            drakeMeshRenderer.Clear(!isStatic);
        }

        public void StartLoading(in DrakeMeshMaterialComponent drakeMeshMaterial) {
            _componentsManager.StartLoading(drakeMeshMaterial);
        }

        public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
            out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
            out UVDistributionMetricComponent uvDistributionMetricComponent) {
            return _componentsManager.TryGetMaterialMesh(drakeMeshMaterial,
                out materialMeshInfo, out mipmapsMaterialComponent, out uvDistributionMetricComponent);
        }

        public void Unload(DrakeMeshMaterialComponent drakeMeshMaterial, bool assumeMaterialIsLoaded = true) {
            _componentsManager.Unload(drakeMeshMaterial, assumeMaterialIsLoaded);
        }

        public void InvalidateEcb() {
            _ecbFrame = -1;
        }

        public Unmanaged GetUnmanaged() {
            return new Unmanaged(_componentsManager.GetUnmanaged());
        }

        void ValidateEcb() {
            if (_ecbFrame != Time.frameCount) {
                _ecb = _beginPresentationEntityCommandBufferSystem.CreateCommandBuffer();
                _ecbFrame = Time.frameCount;
            }
#if UNITY_EDITOR
            // In edit-mode when switching scenes safety can become invalid and blow up the ecb
            if (!Application.isPlaying) {
                try {
                    _ecb.SetName(_dummy, "DrakeDummy");
                } catch {
                    _ecb = _beginPresentationEntityCommandBufferSystem.CreateCommandBuffer();
                    _ecbFrame = Time.frameCount;
                }
            }
#endif
        }

        void ShadowsConfigChanged(in UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> newData) {
            _shadowVolumeData = newData;
        }

        public static SystemRelatedLifeTime<DrakeRendererManager>.IdComponent GetSceneIdComponent(Scene scene) {
            if (!Application.isPlaying) {
                return new SystemRelatedLifeTime<DrakeRendererManager>.IdComponent(scene.handle);
            }

            if (!SceneRemap.TryGetValue(scene.handle, out var remappedScene)) {
                var subscene = GameObjects.FindComponentByTypeInScene<ISubscene>(scene, false);
                if (subscene != null) {
                    remappedScene = subscene.OwnerScene;
                } else {
                    remappedScene = scene;
                }

                SceneRemap[scene.handle] = remappedScene;
            }

            return new SystemRelatedLifeTime<DrakeRendererManager>.IdComponent(remappedScene.handle);
        }

        static bool ProcessFilterSettings(ref RenderFilterSettings filterSettings, in LodGroupSerializableData lodData, int lodMask, in AABB worldAABB, in UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> shadowVolumeData) {
            if (filterSettings.ShadowCastingMode == ShadowCastingMode.On & lodData.isValid) {
                var lodRange = lodData.CreateLODRange(lodMask);
                if (SmallObjectsShadows.ShouldDisableShadows(lodRange, worldAABB, shadowVolumeData)) {
                    filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
                    return true;
                }
            }
            return false;
        }

        static void AsLodGroup(DrakeLodGroup drakeLodGroup) {
            var lodGroup = drakeLodGroup.GameObject.AddComponent<LODGroup>();
            var transform = drakeLodGroup.transform;

            var lodsSerializedData = drakeLodGroup.LodGroupSerializableData;
            var lodSize = drakeLodGroup.LodGroupSize;
            var scale = mathExt.Scale(lodsSerializedData.localToWorldMatrix);
            var worldSize = LodUtils.GetWorldSpaceScale(scale) * lodSize;

            var maxLod = 0;
            (Renderer, int)[] lodUnbakeData = new (Renderer, int)[drakeLodGroup.Renderers.Length];
            for (int i = 0; i < drakeLodGroup.Renderers.Length; i++) {
                var drakeRenderer = drakeLodGroup.Renderers[i];
                var lodMask = drakeRenderer.LodMask;
                var renderer = AsMeshRenderer(transform, drakeRenderer, drakeLodGroup.IsStatic);
                lodUnbakeData[i] = (renderer, lodMask);
                var lastUsedLod = 32 - math.lzcnt(lodMask);
                maxLod = math.max(maxLod, lastUsedLod);
            }
            if (maxLod > 8) {
                Log.Important?.Error("Cannot correctly spawn authoring, too many LODs.");
                maxLod = 8;
            }

            var lods = new LOD[maxLod];
            for (int i = 0; i < maxLod; i++) {
                var mask = 1 << i;
                var renderers = lodUnbakeData.Where(d => (d.Item2 & mask) == mask).Select(static d => d.Item1).ToArray();
                var distance = i < 4 ? lodsSerializedData.lodDistances0[i] : lodsSerializedData.lodDistances1[i - 4];
                lods[i] = new LOD(worldSize / distance, renderers);
            }

            lodGroup.size = lodSize;
            lodGroup.SetLODs(lods);
        }

        static MeshRenderer AsMeshRenderer(Transform parent, DrakeMeshRenderer drakeRenderer, bool isStatic) {
            var mesh = drakeRenderer.MeshReference.LoadAssetAsync<Mesh>().WaitForCompletion();

            var materials = drakeRenderer.MaterialReferences.Select(m => m.LoadAssetAsync<Material>().WaitForCompletion()).ToArray();

            var go = new GameObject("Renderer", typeof(MeshRenderer), typeof(MeshFilter));

            var newTransform = go.transform;
            var drakeMatrix = drakeRenderer.LocalToWorld;
            newTransform.localPosition = mathExt.Translation(drakeMatrix);
            newTransform.localRotation = mathExt.Rotation(drakeMatrix);
            newTransform.localScale = mathExt.Scale(drakeMatrix);

            newTransform.SetParent(parent, true);

            var meshFilter = go.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials;

            var descriptor = drakeRenderer.RenderMeshDescription(isStatic);
            meshRenderer.renderingLayerMask = descriptor.FilterSettings.RenderingLayerMask;
            meshRenderer.shadowCastingMode = descriptor.FilterSettings.ShadowCastingMode;
            meshRenderer.receiveShadows = descriptor.FilterSettings.ReceiveShadows;
            meshRenderer.motionVectorGenerationMode = descriptor.FilterSettings.MotionMode;
            meshRenderer.staticShadowCaster = descriptor.FilterSettings.StaticShadowCaster;
            meshRenderer.lightProbeUsage = descriptor.LightProbeUsage;

            drakeRenderer.Clear(true);

            return meshRenderer;
        }

        public struct Unmanaged {
            DrakeRendererComponentsManager.Unmanaged _componentsManager;

            public Unmanaged(DrakeRendererComponentsManager.Unmanaged componentsManager) {
                _componentsManager = componentsManager;
            }

            public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
                out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
                out UVDistributionMetricComponent uvDistributionMetricComponent) {
                return _componentsManager.TryGetMaterialMesh(drakeMeshMaterial,
                    out materialMeshInfo, out mipmapsMaterialComponent, out uvDistributionMetricComponent);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        static bool IsApplicationPlaying() {
#if UNITY_EDITOR
            return Application.isPlaying;
#else
            return true;
#endif
        }

        [Command("rendering.drake-to-unity", "Changes drake rendering to unity rendering")][UnityEngine.Scripting.Preserve]
        static void DrakeToUnityRenderers() {
            s_toUnityMode = true;
        }

        [Command("rendering.drake-to-drake", "Changes drake unity rendering to drake rendering")][UnityEngine.Scripting.Preserve]
        static void DrakeToDrake() {
            s_toUnityMode = false;
        }
    }
}