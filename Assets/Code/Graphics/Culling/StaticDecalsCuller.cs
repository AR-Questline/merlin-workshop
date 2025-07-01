using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.PackageUtilities.Collections;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.ScriptedEvents;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UniversalProfiling;

namespace Awaken.TG.Graphics.Culling {
    public class StaticDecalsCuller : PlayerLoopBasedLifetimeMonoBehaviour, IDecalsCuller, MipmapsStreamingMasterMaterials.IMipmapsFactorProvider {
        public const float DefaultDrawDistance = 50;
        const byte InitialVisibilityState = 0b0111;
        const int BitMaskIsNotNull = 0b0100;
        const int BitMaskIsVisibilityChanged = 0b0010;
        const int BitMaskIsVisible = 0b0001;
        const int BitMaskIsVisibilityChangedAndNotNull = BitMaskIsVisibilityChanged | BitMaskIsNotNull;

        static readonly UniversalProfilerMarker UpdateDecalsEnabledStateMarker = new("DecalsCuller.UpdateDecalsEnabledState");
        static readonly Dictionary<int, StaticDecalsCuller> SceneHandleToDecalsCullerMap = new(10);

        public float cullingDistanceMultiplier = 1.1f;
        [SerializeField] StructList<DecalProjector> staticDecalProjectors;
        [SerializeField] StructList<float> staticDecalProjectorsDistancesSqs;

        TransformAccessArray _staticDecalsTransforms;
        NativeArray<float4> _staticDecalProjectorsPositions;
        NativeList<float> _drawDistancesSq;
        NativeArray<byte> _staticDecalsVisibilityStates;
        int _staticDecalsCount;
        JobHandle _visibilityStateJobHandle;
        bool _isInitialized;

        // Mipmaps
        bool _registeredMipmaps;

        UnsafeArray<float> _radii;
        UnsafeArray<float> _scalesSq;
        UnsafeArray<float> _reciprocalUvDistributions;
        UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId> _materialIds;

        public string DescriptiveName => $"{gameObject.scene.name}/{name}";

        // === Lifetime
        protected override void OnPlayerLoopEnable() {
            IDecalsCuller.DecalsCullers.Add(this);

            _radii = new UnsafeArray<float>((uint)staticDecalProjectors.Count, ARAlloc.Persistent);
            _scalesSq = new UnsafeArray<float>((uint)staticDecalProjectors.Count, ARAlloc.Persistent);
            _reciprocalUvDistributions = new UnsafeArray<float>((uint)staticDecalProjectors.Count, ARAlloc.Persistent);
            _materialIds = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>((uint)staticDecalProjectors.Count, ARAlloc.Persistent);

            var mipmaps = MipmapsStreamingMasterMaterials.Instance;

            for (var i = 0; i < staticDecalProjectors.Count; i++) {
                var projector = staticDecalProjectors[i];

                var projectorTransform = projector.transform;
                var scale = projector.scaleMode == DecalScaleMode.InheritFromHierarchy ? projectorTransform.lossyScale : Vector3.one;

                var size = projector.size;
                var width = size.x;
                var height = size.y;

                var tilling = projector.uvScale;

                _scalesSq[(uint)i] = math.lengthsq(scale);
                _reciprocalUvDistributions[(uint)i] = (tilling.x * tilling.y) / (width * height);

                var scaledSize = new float3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
                var radius = math.length(scaledSize) * 0.5f;

                _radii[(uint)i] = radius;

                var material = projector.material;
                if (material) {
                    _materialIds[(uint)i] = mipmaps.AddMaterial(material);
                } else {
                    _materialIds[(uint)i] = MipmapsStreamingMasterMaterials.MaterialId.Invalid;
                }
            }

            mipmaps.AddProvider(this);
            _registeredMipmaps = true;
        }

        protected override void OnPlayerLoopDisable() {
            if (!_registeredMipmaps) {
                return;
            }
            var mipmaps = MipmapsStreamingMasterMaterials.Instance;
            for (var i = 0u; i < _materialIds.Length; i++) {
                mipmaps.RemoveMaterial(_materialIds[i]);
            }
            mipmaps.RemoveProvider(this);
            _scalesSq.Dispose();
            _reciprocalUvDistributions.Dispose();
            _materialIds.Dispose();
            _radii.Dispose();
            _registeredMipmaps = false;
        }

        void OnDestroy() {
            if (_registeredMipmaps) {
                OnPlayerLoopDisable();
            }
            UnregisterThis();
            Dispose();
        }
        
        void LateUpdate() {
            _visibilityStateJobHandle.Complete();
            _visibilityStateJobHandle = default;

            if (_staticDecalsCount == 0) {
                return;
            }
            var mainCamera = World.Any<GameCamera>()?.MainCamera;
            if (mainCamera == null) {
                return;
            }


            if (_staticDecalsTransforms.isCreated) {
                _staticDecalsTransforms.Dispose();
            }

            UpdateDecalsEnabledState(in staticDecalProjectors, _staticDecalsVisibilityStates);
            var cameraTransform = mainCamera.transform;
            var cameraPosition = new float4(cameraTransform.position, 0);
            var decalsVisibilityDistanceSq = GetDecalsVisibilityDistanceSq();
            _visibilityStateJobHandle = new CalculateDistanceVisibilityStateJob {
                positions = _staticDecalProjectorsPositions.AsUnsafeSpan(),
                drawDistancesSqs = _drawDistancesSq.AsArray(),
                cameraPosition = cameraPosition,
                hdrpVisibilityDistanceSq = decalsVisibilityDistanceSq,
                distanceMultiplierSq = math.square(cullingDistanceMultiplier),
                outVisibilityStates = _staticDecalsVisibilityStates,
            }.Schedule(_staticDecalsCount, 64);
        }

        void Dispose() {
            IDecalsCuller.DecalsCullers.Remove(this);
            _visibilityStateJobHandle.Complete();
            if (_staticDecalsTransforms.isCreated) {
                _staticDecalsTransforms.Dispose();
            }

            if (_staticDecalProjectorsPositions.IsCreated) {
                AllocationsTracker.CustomFree(_staticDecalProjectorsPositions);
                _staticDecalProjectorsPositions.Dispose();
            }

            if (_staticDecalsVisibilityStates.IsCreated) {
                _staticDecalsVisibilityStates.Dispose();
            }

            if (_drawDistancesSq.IsCreated) {
                _drawDistancesSq.Dispose();
            }
        }

        // === Mipmaps
        public void ProvideMipmapsFactors(in CameraData cameraData, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            if (_staticDecalProjectorsPositions.IsCreated == false) {
                writer.Dispose(default);
                return;
            }
            
            var mipmapsHandle = new DumpMipmapsJob {
                cameraData = cameraData,

                mipmapsIds = _materialIds,
                positions = _staticDecalProjectorsPositions.AsUnsafeSpan(),
                radii = _radii,
                scalesSq = _scalesSq,
                reciprocalUvDistributions = _reciprocalUvDistributions,

                mipmapsWriter = writer,
            }.ScheduleParallel((int)_materialIds.Length, 64, default);
            writer.Dispose(mipmapsHandle);
        }

        [BurstCompile]
        struct DumpMipmapsJob : IJobFor {
            public CameraData cameraData;

            [Unity.Collections.ReadOnly] public UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>.Span mipmapsIds;
            [Unity.Collections.ReadOnly] public UnsafeArray<float4>.Span positions;
            [Unity.Collections.ReadOnly] public UnsafeArray<float>.Span radii;
            [Unity.Collections.ReadOnly] public UnsafeArray<float>.Span scalesSq;
            [Unity.Collections.ReadOnly] public UnsafeArray<float>.Span reciprocalUvDistributions;

            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsWriter;

            public void Execute(int index) {
                var uIndex = (uint)index;

                var mipmapsId = mipmapsIds[uIndex];
                if (mipmapsId == MipmapsStreamingMasterMaterials.MaterialId.Invalid) {
                    return;
                }

                var position = positions[uIndex].xyz;
                var radius = radii[uIndex];
                var scaleSq = scalesSq[uIndex];

                var factorFactor = MipmapsStreamingUtils.CalculateMipmapFactorFactor(cameraData, position, radius, scaleSq);
                var fullFactor = reciprocalUvDistributions[uIndex] * factorFactor;

                mipmapsWriter.UpdateMipFactor(mipmapsId, fullFactor);
            }
        }

        // === Operations
        public void InitSingular() {
            if (TryRegisterThisAndDestroyIfFailed()) {
                EnsureInitialized(false);
                enabled = true;
            }
        }

        static void UpdateDecalsEnabledState(in StructList<DecalProjector> decalProjectors, NativeArray<byte> visibilityStates) {
            UpdateDecalsEnabledStateMarker.Begin();
            int count = decalProjectors.Count;
            for (int i = 0; i < count; i++) {
                int state = visibilityStates[i];
                // is state is not changed or decal is null - skip
                if ((state & BitMaskIsVisibilityChangedAndNotNull) != BitMaskIsVisibilityChangedAndNotNull) {
                    continue;
                }
                var enabledState = (state & BitMaskIsVisible) == 1;
                var decalProjector = decalProjectors[i];
                try {
                    decalProjector.enabled = enabledState;
                } catch {
                    Log.Important?.Error($"Static decal in {nameof(StaticDecalsCuller)} on index {i} was destroyed");
                    state = (state & 0b1111_1011);
                    visibilityStates[i] = (byte)state;
                }
            }
            UpdateDecalsEnabledStateMarker.End();
        }

        // === Merging
        public static void MergeSubscenesCullersIntoOwnerSceneCullerAndEnable() {
            try {
                var allDecalsCullers = FindObjectsByType<StaticDecalsCuller>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < allDecalsCullers.Length; i++) {
                    var decalsCuller = allDecalsCullers[i];
                    bool successfullyRegistered = decalsCuller.TryRegisterThisAndDestroyIfFailed();
                    if (successfullyRegistered) {
                        decalsCuller.EnsureInitialized(true);
                    }
                }

                GetOwnerScenesRequiredCapacityAndSubscenes(
                    out ListDictionary<UnityEngine.SceneManagement.Scene, int> ownerSceneToRequiredCapacityMap,
                    out ListDictionary<int, List<int>> ownerSceneHandleToSubsceneHandleMap);

                foreach (var (ownerScene, requiredCapacity) in ownerSceneToRequiredCapacityMap) {
                    if (SceneHandleToDecalsCullerMap.TryGetValue(ownerScene.handle, out var ownerSceneCuller) == false) {
                        ownerSceneCuller = CreateCullerInOwnerScene(ownerScene);
                    }

                    InitializeOwnerSceneCullerArrays(ownerSceneCuller, requiredCapacity,
                        out NativeArray<float4> staticDecalProjectorsPositions,
                        out NativeList<float> drawDistancesSqs,
                        out NativeArray<byte> staticDecalsVisibilityStates,
                        out DecalProjector[] staticDecalsProjectors,
                        out int countOffset);

                    var subscenesHandles = ownerSceneHandleToSubsceneHandleMap[ownerScene.handle];
                    foreach (var subsceneHandle in subscenesHandles) {
                        MergeSubsceneCullerArraysIntoOwnerSceneCullerArrays(subsceneHandle,
                            staticDecalProjectorsPositions, drawDistancesSqs,
                            staticDecalsVisibilityStates, staticDecalsProjectors, ref countOffset);
                    }

                    SetOwnerSceneCullerArraysToNew(ownerSceneCuller, countOffset, staticDecalProjectorsPositions,
                        drawDistancesSqs, staticDecalsVisibilityStates, staticDecalsProjectors);
                }

                foreach (var decalsCullers in SceneHandleToDecalsCullerMap.Values) {
                    decalsCullers.enabled = true;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                SceneHandleToDecalsCullerMap.Clear();
            }
        }
        
        bool TryScheduleCollectStaticDecalsPositions() {
            if (staticDecalProjectors.Count == 0) {
                _staticDecalsCount = 0;
                return false;
            }
            int staticDecalsInitialCount = staticDecalProjectors.Count;
            _staticDecalsTransforms = new TransformAccessArray(staticDecalsInitialCount);
            var staticDecalsChecked = new StructList<DecalProjector>(staticDecalsInitialCount);
            _drawDistancesSq = new NativeList<float>(staticDecalsInitialCount, ARAlloc.Persistent);
            int staticDecalsCount = 0;
            for (int i = 0; i < staticDecalsInitialCount; i++) {
                var decalProjector = staticDecalProjectors[i];
                if (decalProjector == null) {
                    continue;
                }

                staticDecalsCount++;
                staticDecalsChecked.Add(decalProjector);
                _staticDecalsTransforms.Add(decalProjector.transform);
                _drawDistancesSq.Add(staticDecalProjectorsDistancesSqs[i]);
            }
            _staticDecalsCount = staticDecalsCount;
            staticDecalProjectors = staticDecalsChecked;
            if (staticDecalsCount == 0) {
                staticDecalProjectors = StructList<DecalProjector>.Empty;
                _staticDecalsTransforms.Dispose();
                _drawDistancesSq.Dispose();
                return false;
            }

            _staticDecalProjectorsPositions = new NativeArray<float4>(staticDecalsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            AllocationsTracker.CustomAllocation(_staticDecalProjectorsPositions);
            _staticDecalsVisibilityStates = new NativeArray<byte>(staticDecalsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            unsafe {
                UnsafeUtility.MemSet(_staticDecalsVisibilityStates.GetUnsafePtr(), InitialVisibilityState, staticDecalsCount);
            }

            var pivots = new UnsafeArray<float3>((uint)staticDecalsCount, ARAlloc.Temp);
            var scales = new UnsafeArray<float3>((uint)staticDecalsCount, ARAlloc.Temp);
            for (int i = 0; i < staticDecalsCount; i++) {
                var projector = staticDecalsChecked[i];
                var scale = projector.scaleMode == DecalScaleMode.InheritFromHierarchy ? projector.transform.lossyScale : Vector3.one;
                var pivot = projector.pivot;

                pivots[(uint)i] = pivot;
                scales[(uint)i] = scale;
            }

            var positionsJobHandle = new CollectPositionsJob() {
                pivots = pivots,
                scales = scales,
                outPositions = _staticDecalProjectorsPositions.AsUnsafeSpan(),
            }.Schedule(_staticDecalsTransforms);
            positionsJobHandle.Complete();
            pivots.Dispose();
            scales.Dispose();
            return true;
        }
        
        int EnsureInitialized(bool destroyAndUnregisterIfNoDecals) {
            if (_isInitialized) {
                return _staticDecalsCount;
            }
            _isInitialized = true;
            if (IsRegistered() == false) {
                return 0;
            }
            bool hasAnyDecals = TryScheduleCollectStaticDecalsPositions();
            if (hasAnyDecals == false && destroyAndUnregisterIfNoDecals) {
                UnregisterThis();
                Destroy(this);
                return 0;
            }
            return _staticDecalsCount;
        }

        static void GetOwnerScenesRequiredCapacityAndSubscenes(
            out ListDictionary<UnityEngine.SceneManagement.Scene, int> ownerSceneToRequiredCapacityMap, 
            out ListDictionary<int, List<int>> ownerSceneHandleToSubsceneHandleMap) {
            ownerSceneToRequiredCapacityMap = new ListDictionary<UnityEngine.SceneManagement.Scene, int>(2);
            ownerSceneHandleToSubsceneHandleMap = new ListDictionary<int, List<int>>(2);
            // Copy sceneHandleToDecalsCullerMap to array because when calling
            // DecalsCuller.EnsureInitialized - this dictionary could be modified 
            var sceneHandleToDecalsCullerMapArr = SceneHandleToDecalsCullerMap.ToArray();
            foreach (var (sceneHandle, culler) in sceneHandleToDecalsCullerMapArr) {
                // If it is not a subscene  - skip
                if (ISubscene.SceneHandleToSubsceneMap.TryGetValue(sceneHandle, out var subscene) == false) {
                    continue;
                }
                var ownerScene = subscene.OwnerScene;
                // If it is Static Subdivided Scene (which is a subscene but it is owner of itself) - skip
                if (ownerScene.handle == sceneHandle) {
                    continue;
                }
                var cullerStaticDecalsCount = culler.EnsureInitialized(true);
                if (cullerStaticDecalsCount == 0) {
                    continue;
                }
                if (ownerSceneToRequiredCapacityMap.TryGetValue(ownerScene, out var ownerSceneRequiredCapacity) == false) {
                    ownerSceneRequiredCapacity = 0;
                    ownerSceneToRequiredCapacityMap.Add(ownerScene, ownerSceneRequiredCapacity);
                }
                ownerSceneToRequiredCapacityMap[ownerScene] = ownerSceneRequiredCapacity + culler._staticDecalsCount;
                if (ownerSceneHandleToSubsceneHandleMap.TryGetValue(ownerScene.handle, out var subscenesList) == false) {
                    subscenesList = new List<int>(10);
                    ownerSceneHandleToSubsceneHandleMap.Add(ownerScene.handle, subscenesList);
                }
                subscenesList.Add(sceneHandle);
            }
        }

        static StaticDecalsCuller CreateCullerInOwnerScene(UnityEngine.SceneManagement.Scene ownerScene) {
            var ownerSceneCullerGO = new GameObject("DecalsCuller");
            SceneManager.MoveGameObjectToScene(ownerSceneCullerGO, ownerScene);
            ownerSceneCullerGO.SetActive(false);
            var ownerSceneCuller = ownerSceneCullerGO.AddComponent<StaticDecalsCuller>();
            ownerSceneCuller.enabled = false;
            ownerSceneCullerGO.SetActive(true);
            ownerSceneCuller.TryRegisterThisAndDestroyIfFailed();
            ownerSceneCuller.EnsureInitialized(false);
            return ownerSceneCuller;
        }
        
        static void InitializeOwnerSceneCullerArrays(StaticDecalsCuller ownerSceneCuller, int requiredCapacity,
            out NativeArray<float4> staticDecalProjectorsPositions, out NativeList<float> drawDistancesSqs,
            out NativeArray<byte> staticDecalsVisibilityStates, out DecalProjector[] staticDecalsProjectors, out int countOffset) {
            bool hasAnyValues = ownerSceneCuller._staticDecalProjectorsPositions.IsCreated;
            if (hasAnyValues == false) {
                staticDecalProjectorsPositions = new NativeArray<float4>(requiredCapacity, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                drawDistancesSqs = new NativeList<float>(requiredCapacity, ARAlloc.Persistent);
                staticDecalsVisibilityStates = new NativeArray<byte>(requiredCapacity, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                staticDecalsProjectors = new DecalProjector[requiredCapacity];
                countOffset = 0;
            } else {
                var ownerSceneDecalsCount = ownerSceneCuller._staticDecalsCount;
                var sumRequiredCapacity = ownerSceneDecalsCount + requiredCapacity;
                staticDecalProjectorsPositions = new NativeArray<float4>(sumRequiredCapacity, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                drawDistancesSqs = new NativeList<float>(sumRequiredCapacity, ARAlloc.Persistent);
                staticDecalsVisibilityStates = new NativeArray<byte>(sumRequiredCapacity, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                staticDecalsProjectors = new DecalProjector[sumRequiredCapacity];
                NativeArray<float4>.Copy(ownerSceneCuller._staticDecalProjectorsPositions, 0, staticDecalProjectorsPositions, 0, ownerSceneDecalsCount);
                drawDistancesSqs.AddRange(ownerSceneCuller._drawDistancesSq.AsArray());
                NativeArray<byte>.Copy(ownerSceneCuller._staticDecalsVisibilityStates, 0, staticDecalsVisibilityStates, 0, ownerSceneDecalsCount);
                Array.Copy(ownerSceneCuller.staticDecalProjectors.BackingArray, 0, staticDecalsProjectors, 0, ownerSceneDecalsCount);
                countOffset = ownerSceneDecalsCount;
            }
        }
        
        static void MergeSubsceneCullerArraysIntoOwnerSceneCullerArrays(int subsceneHandle, NativeArray<float4> staticDecalProjectorsPositions,
            NativeList<float> drawDistancesSqs, NativeArray<byte> staticDecalsVisibilityStates,
            DecalProjector[] staticDecalsProjectors, ref int countOffset
            ) {
            var subsceneCuller = SceneHandleToDecalsCullerMap[subsceneHandle];
            if (subsceneCuller == null) {
                Log.Important?.Error($"Distance culler for scene with handle {subsceneHandle} was destroyed");
                return;
            }
            var subsceneCullerDecalsCount = subsceneCuller._staticDecalsCount; 
            NativeArray<float4>.Copy(subsceneCuller._staticDecalProjectorsPositions, 0, staticDecalProjectorsPositions, countOffset, subsceneCullerDecalsCount);
            drawDistancesSqs.AddRange(subsceneCuller._drawDistancesSq.AsArray());
            NativeArray<byte>.Copy(subsceneCuller._staticDecalsVisibilityStates, 0, staticDecalsVisibilityStates, countOffset, subsceneCullerDecalsCount);
            Array.Copy(subsceneCuller.staticDecalProjectors.BackingArray, 0, staticDecalsProjectors, countOffset, subsceneCullerDecalsCount);
            countOffset += subsceneCullerDecalsCount;
            SceneHandleToDecalsCullerMap.Remove(subsceneHandle);
            Destroy(subsceneCuller);
        }
        
        static void SetOwnerSceneCullerArraysToNew(StaticDecalsCuller ownerSceneCuller, int decalsCount,
            NativeArray<float4> staticDecalProjectorsPositions, NativeList<float> drawDistancesSqs,
            NativeArray<byte> staticDecalsVisibilityStates, DecalProjector[] staticDecalsProjectors) {
            ownerSceneCuller._staticDecalsCount = decalsCount;

            if (ownerSceneCuller._staticDecalProjectorsPositions.IsCreated) {
                AllocationsTracker.CustomFree(ownerSceneCuller._staticDecalProjectorsPositions);
                ownerSceneCuller._staticDecalProjectorsPositions.Dispose();
            }
            AllocationsTracker.CustomAllocation(staticDecalProjectorsPositions);
            ownerSceneCuller._staticDecalProjectorsPositions = staticDecalProjectorsPositions;

            ownerSceneCuller._staticDecalsVisibilityStates.Dispose();
            ownerSceneCuller._staticDecalsVisibilityStates = staticDecalsVisibilityStates;
            ownerSceneCuller.staticDecalProjectors = new StructList<DecalProjector>(staticDecalsProjectors);
            ownerSceneCuller._drawDistancesSq.Dispose();
            ownerSceneCuller._drawDistancesSq = drawDistancesSqs;
        }

        // === Helpers
        bool IsRegistered() {
            return SceneHandleToDecalsCullerMap.ContainsKey(gameObject.scene.handle);
        }

        bool TryRegisterThisAndDestroyIfFailed() {
            var sceneHandle = gameObject.scene.handle;
            if (SceneHandleToDecalsCullerMap.TryAdd(sceneHandle, this) == false) {
                Log.Important?.Error($"More than one {nameof(StaticDecalsCuller)} in scene {gameObject.scene.name}. Destroying {nameof(StaticDecalsCuller)} {gameObject.name}");
                Destroy(this);
                return false;
            }

            return true;
        }

        void UnregisterThis() {
            if (SceneHandleToDecalsCullerMap.TryGetValue(gameObject.scene.handle, out var registeredCuller) && registeredCuller == this) {
                SceneHandleToDecalsCullerMap.Remove(gameObject.scene.handle);
            }
        }

        float GetDecalsVisibilityDistanceSq() {
            var currentHDRenderPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as HDRenderPipelineAsset;
            if (currentHDRenderPipelineAsset == null) {
                return 200;
            }

            var renderPipelineSettings = currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings;
            var decalSettings = renderPipelineSettings.decalSettings;
            var hdrpSettingsDecalsDrawDistance = decalSettings.drawDistance;
            return math.square(hdrpSettingsDecalsDrawDistance) * cullingDistanceMultiplier;
        }
        
#if UNITY_EDITOR
        static readonly Type[] NonStaticComponents = new Type[] {
            typeof(SceneSpec),
            typeof(NpcInteractionBase),
            typeof(GroupInteraction),
            typeof(DistanceCullerGroup),
            typeof(ScriptedEvent),
        };

        [Button]
        public void EDITOR_FillFromScene() {
            var scene = gameObject.scene;
            var decalProjectors = GameObjects.FindComponentsByTypeInScene<DecalProjector>(scene, false, 100);

            staticDecalProjectors = new StructList<DecalProjector>(decalProjectors.Count);
            staticDecalProjectorsDistancesSqs = new StructList<float>(decalProjectors.Count);

            foreach (var decalProjector in decalProjectors) {
                var decalGameObject = decalProjector.gameObject;
                if (!decalGameObject.isStatic) {
                    continue;
                }
                if (decalGameObject.hideFlags != HideFlags.None) {
                    continue;
                }
                if (decalGameObject.HasAnyComponentInParent(NonStaticComponents)) {
                    var parentLocationSpec = decalGameObject.GetComponentInParent<LocationSpec>();
                    if (parentLocationSpec == null || !IsConsideredAsStaticLocation(parentLocationSpec)) {
                        continue;
                    }
                }
                staticDecalProjectors.Add(decalProjector);

                var decalDistance = math.min(decalProjector.drawDistance, DefaultDrawDistance);
                var decalDistanceExtent = decalProjector.GetComponent<DecalDistanceExtent>();
                if (decalDistanceExtent) {
                    decalDistance = decalDistanceExtent.distance;
                    DestroyImmediate(decalDistanceExtent);
                }

                decalProjector.drawDistance = decalDistance;
                decalProjector.fadeScale = 0.65f;

                staticDecalProjectorsDistancesSqs.Add(math.square(decalDistance));
            }
            enabled = false;
            UnityEditor.EditorUtility.SetDirty(this);

            bool IsConsideredAsStaticLocation(LocationSpec location) {
                if (location.IsHidableStatic) {
                    return true;
                }
                var prefabReference = location.prefabReference;
                return prefabReference is not { IsSet: true };
            }
        }
#endif
        
        [Serializable]
        public class DecalsProjectorsList : UnsafePinnableList<DecalProjector> {
            public DecalsProjectorsList(int capacity) : base(capacity) { }
            public DecalsProjectorsList() { }
        }
    }
}