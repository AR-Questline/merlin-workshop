
using System;
using System.Collections.Generic;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.Components;
using Awaken.ECS.Flocks.Authorings;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Flocks {
    public class FlockRestSpot : MonoBehaviour {
        [SerializeField] FlockGroup flockGroup;
        [SerializeField] float radius = 20.0f;
        [SerializeField] Vector2 autoCatchDelayMinMax = new(10.0f, 20.0f);
        [SerializeField] Vector2 autoDismountDelayMinMax = new(10.0f, 20.0f);
        [UnityEngine.Scripting.Preserve] public Entity Entity => _entity;
        
        Entity _entity;

        void Awake() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entity = entityManager.CreateEntity(
                ComponentType.ReadWrite<FlockRestSpotData>(),
                ComponentType.ReadWrite<FlockRestSpotStatus>(),
                ComponentType.ReadWrite<FlockRestSpotTimeData>(),
                ComponentType.ReadWrite<RestSpotTryFindOrRelease>(),
                ComponentType.ReadWrite<LinkedEntitiesAccessRequest>());
            var hash = math.hash(new uint2(gameObject.GetHashCode()));
            var random = new Unity.Mathematics.Random(hash);
            var initialElapsedTime = random.NextFloat(0, autoCatchDelayMinMax.y);
            entityManager.SetComponentData(_entity, new FlockRestSpotTimeData(hash, initialElapsedTime));
            var linkedEntityLifetime = LinkedEntityLifetime.GetOrCreate(gameObject);
            entityManager.SetComponentData(_entity, new LinkedEntitiesAccessRequest(linkedEntityLifetime));
            if (flockGroup != null) {
                SetFlockGroup(flockGroup, entityManager);
            }
        }

        void Start() {
            Destroy(this);
        }

        public void SetFlockGroup(FlockGroup flockGroup, EntityManager entityManager) {
            this.flockGroup = flockGroup;
            if (_entity != Entity.Null) {
                entityManager.SetComponentData(_entity, new FlockRestSpotData(
                    flockGroup.FlockGroupEntity, transform.position, radius, autoCatchDelayMinMax, autoDismountDelayMinMax));
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = Color.white;
        }
        
        [Button]
        public static void ConnectAllToNearestFlockGroups() {
            var restSpots = Object.FindObjectsByType<FlockRestSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var notSetRestSpots = new List<FlockRestSpot>(); 
            for (int i = 0; i < restSpots.Length; i++) {
                if (restSpots[i].flockGroup == null) {
                    notSetRestSpots.Add(restSpots[i]);
                }
            }

            if (notSetRestSpots.Count == 0) {
                return;
            }
            var flockGroups = Object.FindObjectsByType<FlockGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var flocksGroupsPositions = new NativeArray<float3>(flockGroups.Length, ARAlloc.Temp);
            for (int i = 0; i < flocksGroupsPositions.Length; i++) {
                flocksGroupsPositions[i] = flockGroups[i].transform.position;
            }

            if (flocksGroupsPositions.Length == 0) {
                Log.Important?.Error("No Flock groups in open scenes. Cannot connect rest spots");
                return;
            }
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            for (int i = 0; i < notSetRestSpots.Count; i++) {
                var restSpot = notSetRestSpots[i];
                restSpot.SetFlockGroup(flockGroups[GetNearestPositionIndex(restSpot.transform.position, flocksGroupsPositions)], entityManager);
                UnityEditor.EditorUtility.SetDirty(restSpot);
            }

            static int GetNearestPositionIndex(float3 position, NativeArray<float3> positions) {
                int nearestPosIndex = -1;
                float nearestPosDistSq = float.PositiveInfinity;
                for (int i = 0; i < positions.Length; i++) {
                    var distSq = math.distancesq(position, positions[i]);
                    if (distSq < nearestPosDistSq) {
                        nearestPosDistSq = distSq;
                        nearestPosIndex = i;
                    }
                }

                return nearestPosIndex;
            }
        }

        [Button]
        public static void SetFlocksScaringDistance(float scaringDistance) {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FlockScareFromRestSpotsSystem>().FlocksScaringDistance = scaringDistance;
        }
#endif
    }
}