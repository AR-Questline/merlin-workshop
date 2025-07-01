using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.ECS.Elevator;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;
using World = Unity.Entities.World;

namespace Awaken.TG.Main.Locations.Elevator {
    public class ElevatorChains : ViewComponent<Location> {
        const int ChainsPreallocateCount = 20;
        static readonly UniversalProfilerMarker ChainGeneration = new UniversalProfilerMarker("Elevator.Chains");

#if UNITY_EDITOR
        [OnValueChanged(nameof(EDITOR_Clear))]
#endif
        [SerializeField, Delayed, Min(1)]
        float maxChainY;

#if UNITY_EDITOR
        [OnValueChanged(nameof(EDITOR_Clear))]
#endif
        [ChildGameObjectsOnly, DelayedProperty]
        public Transform[] chainPositions = new Transform[4];

        [AssetsOnly, Required]
        public GameObject chain;

#if UNITY_EDITOR
        [OnValueChanged(nameof(EDITOR_Clear))]
#endif
        [SerializeField, MaxValue(nameof(singleChainHeight)), Delayed]
        float chainSpacingAdjustment;

        [Space]
        [ShowInInspector, NonSerialized, Sirenix.OdinInspector.ReadOnly]
        public float platformHeight;

        [Sirenix.OdinInspector.ReadOnly]
        public float singleChainHeight;

        Location ParentLocation => Target;

#if UNITY_EDITOR
        public int EDITOR_InitializedSlotsCount => _spawnedChains?.Length ?? 0;

        [ShowInInspector] public float PlatformYPosition_Expensive => transform.position.y;

        [Space]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        float _totalChainHeight;

        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        float _actualChainHeight;

        [Space]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        int _spawnedChainCount;

        [ShowInInspector, ListDrawerSettings(IsReadOnly = true), InlineButton(nameof(EDITOR_Clear))]
        List<GameObject>[] _spawnedChains;
#endif

        ElevatorPlatformCurrentPositionY _platformCurrentPositionY;
        NativeArray<Entity> _chainEntitiesPrefabs;
        Entity _chainDataEntity = Entity.Null;
        IEventListener _onLocationInitializedListener;

        protected override void OnAttach() {
            base.OnAttach();
            if (GenericTarget is not Location location || !location.HasElement<ElevatorPlatform>()) {
                Log.Important?.Error("Elevator chains cannot find elevator! " + GenericTarget?.ContextID, this);
                return;
            }

            if (singleChainHeight <= 0) {
                Log.Important?.Error("Elevator chain model collider error", this);
                return;
            }

            ParentLocation.GetOrCreateTimeDependent().WithUpdate(UpdateChains);
            InitChainContainers();
        }

        public void InitChainContainers() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                _spawnedChains = new List<GameObject>[chainPositions.Length];
                for (int i = 0; i < _spawnedChains.Length; i++) {
                    _spawnedChains[i] = new List<GameObject>(20);
                }

                return;
            }
#endif
            _onLocationInitializedListener = Target.ListenTo(Model.Events.AfterFullyInitialized, RuntimeInitChainContainers, this);
        }

        void RuntimeInitChainContainers() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _chainDataEntity = entityManager.CreateEntity(
                ComponentType.ReadWrite<ElevatorChainData>(),
                ComponentType.ReadWrite<DrakeEntityPrefab>(),
                ComponentType.ReadWrite<DrakeStaticPrefabData>(),
                ComponentType.ReadWrite<ElevatorPlatformCurrentPositionY>());

            var options = new IWithUnityRepresentation.Options {
                linkedLifetime = false,
                movable = false
            };

            var chainPrefabInstanceDrakeLodGroupStatic = DrakeRuntimeSpawning.InstantiatePrefab(chain, options);
            var chainPrefabGO = chainPrefabInstanceDrakeLodGroupStatic.gameObject;

            DrakeRuntimeSpawning.CreateAndAddDrakeEntityPrefabs(chainPrefabInstanceDrakeLodGroupStatic, gameObject.scene, _chainDataEntity, entityManager,
                ARAlloc.Persistent, out _chainEntitiesPrefabs);
            Destroy(chainPrefabGO);
            var platformTransform = ParentLocation.ViewParent;
            var platformCurrentPosition = platformTransform.position;

            int chainPositionsCount = chainPositions.Length;
            var chainRootsLocalPositions = new NativeArray<float3>(chainPositionsCount, ARAlloc.Persistent);
            for (int i = 0; i < chainPositionsCount; i++) {
                var chainRootLocalPosition = chainPositions[i].position - platformCurrentPosition;
                chainRootsLocalPositions[i] = chainRootLocalPosition;
            }

            float finalSingleChainHeight = singleChainHeight - chainSpacingAdjustment;
            var platformRotation = platformTransform.rotation;
            if (platformCurrentPosition.y > maxChainY) {
                GameObject platformTransformGameObject = platformTransform.gameObject;
#if UNITY_EDITOR
                Log.Important?.Error($"Elevator platform {platformTransform.name} initial position y: {platformCurrentPosition.y} is higher than {nameof(maxChainY)}: {maxChainY}. Move platform lower or increase {nameof(maxChainY)}. Setting {nameof(maxChainY)} to {platformCurrentPosition.y} >> {platformTransformGameObject.PathInSceneHierarchy()}", platformTransformGameObject);
#endif
                maxChainY = platformCurrentPosition.y;
            }
            entityManager.SetComponentData(_chainDataEntity, new ElevatorChainData(
                platformCurrentPosition, platformRotation, maxChainY,
                finalSingleChainHeight, chainRootsLocalPositions, ChainsPreallocateCount * _chainEntitiesPrefabs.Length));
            entityManager.SetComponentData(_chainDataEntity, new ElevatorPlatformCurrentPositionY(platformCurrentPosition.y));
        }

        void UpdateChains(float _) {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                platformHeight = ParentLocation.Coords.y;
                EDITOR_HandleChainGeneration();
                return;
            }
#endif
            if (_chainDataEntity == Entity.Null) {
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var prevPlatformPosition = entityManager.GetComponentData<ElevatorPlatformCurrentPositionY>(_chainDataEntity);
            var currentPositionY = ParentLocation.Coords.y;
            if (prevPlatformPosition.value != currentPositionY) {
                entityManager.SetComponentData(_chainDataEntity, new ElevatorPlatformCurrentPositionY(currentPositionY));
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (_chainEntitiesPrefabs.IsCreated) {
                foreach (var chainEntityPrefab in _chainEntitiesPrefabs) {
                    entityManager.DestroyEntity(chainEntityPrefab);
                }
                _chainEntitiesPrefabs.Dispose();
            }

            if (_chainDataEntity != Entity.Null) {
                var spawnedEntities = entityManager.GetComponentData<ElevatorChainData>(_chainDataEntity).spawnedEntities;
                if (spawnedEntities.IsCreated) {
                    entityManager.DestroyEntity(spawnedEntities.AsArray());
                    spawnedEntities.Dispose();
                }

                entityManager.DestroyEntity(_chainDataEntity);
            }

            Awaken.TG.MVC.World.EventSystem.TryDisposeListener(ref _onLocationInitializedListener);
        }

#if UNITY_EDITOR
        public void EDITOR_HandleChainGeneration() {
            if (_spawnedChains == null) return;

            ChainGeneration.Begin();
            _totalChainHeight = maxChainY - platformHeight;
            float finalSingleChainHeight = singleChainHeight - chainSpacingAdjustment;


            int wantedChainCount = (int)math.ceil(_totalChainHeight / finalSingleChainHeight);
            if (_spawnedChainCount == wantedChainCount) {
                ChainGeneration.End();
                return;
            }

            _actualChainHeight = finalSingleChainHeight * wantedChainCount;
            int chainsToSpawn = wantedChainCount - _spawnedChainCount;

            if (chainsToSpawn < 0) {
                for (int i = 0; i < _spawnedChains.Length; i++) {
                    for (int j = chainsToSpawn; j < 0; j++) {
                        if (_spawnedChains[i].Count == 0) continue;
                        EDITOR_RemoveChainInstance(i);
                        _spawnedChains[i].RemoveAt(_spawnedChains[i].Count - 1);
                    }
                }
            } else {
                for (int i = 0; i < _spawnedChains.Length; i++) {
                    for (int j = 0; j < chainsToSpawn; j++) {
                        GameObject spawnedChain = EDITOR_GetChainInstance(i);
                        spawnedChain.transform.position += Vector3.up * (finalSingleChainHeight * _spawnedChains[i].Count);
                        _spawnedChains[i].Add(spawnedChain);
                    }
                }
            }

            _spawnedChainCount = wantedChainCount;
            ChainGeneration.End();
        }

        GameObject EDITOR_GetChainInstance(int i) {
            GameObject instantiate = Instantiate(chain, chainPositions[i], false);
            instantiate.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = true,
            });
            if (!Application.isPlaying) {
                instantiate.hideFlags = HideFlags.HideAndDontSave;
            }

            return instantiate;
        }

        void EDITOR_RemoveChainInstance(int i) {
            GameObjects.DestroySafely(_spawnedChains[i][^1]);
        }

        public void EDITOR_Clear() {
            if (_spawnedChains == null || Application.isPlaying) return;
            for (int i = 0; i < _spawnedChains.Length; i++) {
                for (int j = 0; j < _spawnedChains[i].Count; j++) {
                    DestroyImmediate(_spawnedChains[i][j]);
                }
            }

            _spawnedChains = null;
            _spawnedChainCount = 0;
            _actualChainHeight = 0;
            _totalChainHeight = 0;
        }
#endif
        protected override void OnDiscard() {
            ParentLocation.GetTimeDependent()?.WithoutUpdate(UpdateChains);
            base.OnDiscard();
        }
    }
}