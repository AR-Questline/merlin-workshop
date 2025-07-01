using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TAO.VertexAnimation {
    [UpdateInGroup(typeof(VertexAnimationSystemGroup))]
    [UpdateAfter(typeof(StartAnimationTransitionSystem))]
    [UpdateBefore(typeof(SetAnimationDataMaterialPropertiesSystem))]
    [UpdateBefore(typeof(StepCurrentAnimationSystem))]
    public partial class StepTransitionAnimationSystem : SystemBase {
        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(CheckedStateRef.WorldUnmanaged);
            Dependency = Entities.ForEach((Entity entity, ref VA_CurrentAnimationData animationData,
                ref VA_SmoothTransitionData smoothTransitionData) => {
                
                var currentAnimDelta= deltaTime * animationData.AnimationSpeed;
                animationData.AnimationTime = (animationData.AnimationTime + currentAnimDelta) % animationData.AnimationDuration;
                
                var nextAnimDelta = deltaTime * smoothTransitionData.NextAnimationSpeed;
                smoothTransitionData.NextAnimationTime = (smoothTransitionData.NextAnimationTime + nextAnimDelta) % smoothTransitionData.NextAnimationDuration;
                
                smoothTransitionData.ElapsedTransitionTime += deltaTime;

                if (smoothTransitionData.ElapsedTransitionTime > smoothTransitionData.TransitionTime) {
                    animationData.AnimationIndex = smoothTransitionData.NextAnimationIndex;
                    animationData.AnimationSpeed = smoothTransitionData.NextAnimationSpeed;
                    animationData.AnimationDuration = smoothTransitionData.NextAnimationDuration;
                    animationData.AnimationTime = smoothTransitionData.NextAnimationTime;
                    // Set Lerp to 0
                    smoothTransitionData.ElapsedTransitionTime = 0;
                    smoothTransitionData.TransitionTime = 1;
                    ecb.RemoveComponent<VA_SmoothTransitionData>(entity);
                }
            }).Schedule(Dependency);
        }
    }
}