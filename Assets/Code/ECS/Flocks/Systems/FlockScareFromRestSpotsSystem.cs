using Awaken.CommonInterfaces;
using Awaken.ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(VariableRateHalfSecondFlockSystemGroup))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlockScareFromRestSpotsSystem : SystemBase {
        public float FlocksScaringDistance { get; internal set; } = 20;
        
        ComponentLookup<TargetParams> _targetParamsLookup;
        EntityQuery _restSpotsQuery;
        FlockRestSpotSystem _restSpotSystem;
        protected override void OnCreate() {
            base.OnCreate();
            _targetParamsLookup = GetComponentLookup<TargetParams>();
            _restSpotsQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockRestSpotData>().WithPresentRW<FlockRestSpotStatus>()
                .WithNone<CulledEntityTag>().Build();
            _restSpotSystem = World.GetOrCreateSystemManaged<FlockRestSpotSystem>();
        }

        protected override void OnUpdate() {
            if (SystemAPI.Time.DeltaTime == 0) {
                return;
            }

            if (_restSpotSystem.ForceSpotsActive) {
                return;
            }
            // Using elapsed time from other system because elapsed time in variable rate systems is different
            var elapsedTime = _restSpotSystem.ElapsedTime;
            var allowedLandingTime = elapsedTime + ReleaseEntitiesOnSpotsJob.BlockLandingTime;
            _targetParamsLookup.Update(this);
            var heroPosition = HeroPosition.Value;
            Dependency = new TryScareFlockEntitiesFromRestSpotsJob() {
                targetParamsLookup = _targetParamsLookup,
                scareTransformPosition = heroPosition,
                allowedLandingTime = allowedLandingTime,
                scaringDistanceSq = math.square(FlocksScaringDistance)
            }.Schedule(_restSpotsQuery, Dependency);
        }

        [BurstCompile]
        partial struct TryScareFlockEntitiesFromRestSpotsJob : IJobEntity {
            public ComponentLookup<TargetParams> targetParamsLookup;
            public float3 scareTransformPosition;
            public float allowedLandingTime;
            public float scaringDistanceSq;
            void Execute(ref FlockRestSpotStatus restSpotStatus, in FlockRestSpotData restSpotData) {
                if (math.distancesq(restSpotData.position, scareTransformPosition) < scaringDistanceSq && restSpotStatus.HasEntity) {
                    ReleaseEntitiesOnSpotsJob.ExecuteIml(ref restSpotStatus, ref targetParamsLookup, in allowedLandingTime);
                }
            }
        }
    }
}