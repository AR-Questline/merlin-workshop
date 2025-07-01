using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TAO.VertexAnimation {
    [UpdateInGroup(typeof(VertexAnimationSystemGroup))]
    public partial class StepCurrentAnimationSystem : SystemBase {
        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            Dependency = Entities
                .WithNone<VA_SmoothTransitionData>()
                .ForEach((ref VA_CurrentAnimationData animationData) => {
                    var currentAnimDelta= deltaTime * animationData.AnimationSpeed;
                    animationData.AnimationTime = (animationData.AnimationTime + currentAnimDelta) % animationData.AnimationDuration;
                })
                .Schedule(Dependency);
        }
    }
}