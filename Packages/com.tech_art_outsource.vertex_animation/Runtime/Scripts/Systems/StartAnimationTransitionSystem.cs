using Unity.Entities;
using Unity.Mathematics;

namespace TAO.VertexAnimation {
    [UpdateInGroup(typeof(VertexAnimationSystemGroup))]
    public partial class StartAnimationTransitionSystem : SystemBase {
        protected override void OnUpdate() {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(CheckedStateRef.WorldUnmanaged);
            foreach (var (animationDataRef, sharedAnimationData, animatorParams, entity) in SystemAPI
                         .Query<RefRW<VA_CurrentAnimationData>, VA_SharedAnimationData, VA_AnimatorParams>()
                         .WithNone<VA_SmoothTransitionData>()
                         .WithEntityAccess()) {
                ref VA_AnimationBookData animationsRef = ref sharedAnimationData.AnimationsDatasRef.Value;
                var animationData = animationDataRef.ValueRO;
                var currentAnimationIndex = animationData.AnimationIndex;
                var targetAnimationIndex =
                    math.abs(animatorParams.targetAnimationIndex) % animationsRef.animationsDatas.Length;
                if (currentAnimationIndex != targetAnimationIndex) {
                    var nextAnimationDuration = animationsRef.animationsDatas[targetAnimationIndex].duration;
                    var smoothTransitionData = new VA_SmoothTransitionData() {
                        NextAnimationIndex = (byte)targetAnimationIndex,
                        NextAnimationTime = 0,
                        NextAnimationDuration = nextAnimationDuration,
                        NextAnimationSpeed = animatorParams.targetAnimationSpeed,
                        ElapsedTransitionTime = 0,
                        TransitionTime = currentAnimationIndex == byte.MaxValue ? 0 : animatorParams.transitionTime
                    };
                    ecb.AddComponent(entity, smoothTransitionData);
                } else if(animationData.AnimationSpeed != animatorParams.targetAnimationSpeed) {
                    animationDataRef.ValueRW.AnimationSpeed = animatorParams.targetAnimationSpeed;
                }
            }
        }
    }
}