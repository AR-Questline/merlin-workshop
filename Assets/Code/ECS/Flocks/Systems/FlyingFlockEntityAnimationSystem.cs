using Awaken.ECS.Components;
using TAO.VertexAnimation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup))]
    [UpdateAfter(typeof(FlyingFlockEntityStateSystem))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlyingFlockEntityAnimationSystem : SystemBase {
        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            Dependency = Entities.WithNone<CulledEntityTag>().ForEach((Entity entity, ref FlockAnimatorParams animatorParams,
                in FlyingFlockEntityState state, in FlyingFlockEntityAnimationsData animationsData) => {
                animatorParams.value.targetAnimationSpeed = 1;
                animatorParams.value.transitionTime = animationsData.transitionTime;

                var animationState = state.value &
                                     (FlyingFlockEntityState.State.Flapping | FlyingFlockEntityState.State.Soaring | FlyingFlockEntityState.State.Resting);

                var entityStableHash = math.hash(new int2(entity.Index, entity.Version));
                var flapSpeed = StatelessRandom.GetRandomTime(animationsData.flapSpeedMinMax, entityStableHash);

                switch (animationState) {
                    case FlyingFlockEntityState.State.Flapping: {
                        animatorParams.value.targetAnimationSpeed = flapSpeed;
                        animatorParams.value.targetAnimationIndex = animationsData.flapAnimationIndex;
                        break;
                    }
                    case FlyingFlockEntityState.State.Soaring: {
                        animatorParams.value.targetAnimationSpeed = 1;
                        animatorParams.value.targetAnimationIndex = animationsData.soarAnimationIndex;
                        break;
                    }
                    case FlyingFlockEntityState.State.Resting: {
                        animatorParams.value.targetAnimationSpeed = 1;
                        animatorParams.value.targetAnimationIndex = animationsData.restAnimationIndex;
                        break;
                    }
                }
            }).WithBurst().Schedule(Dependency);
        }
    }
}