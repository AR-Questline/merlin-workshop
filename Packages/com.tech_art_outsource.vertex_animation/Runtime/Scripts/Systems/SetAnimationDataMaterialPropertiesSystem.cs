using Unity.Entities;
using Unity.Mathematics;

namespace TAO.VertexAnimation {
    [UpdateInGroup(typeof(VertexAnimationSystemGroup))]
    [UpdateAfter(typeof(StepTransitionAnimationSystem))]
    public partial class SetAnimationDataMaterialPropertiesSystem : SystemBase {
        protected override void OnUpdate() {
            Dependency = Entities
                .WithNone<VA_SmoothTransitionData>()
                .ForEach((ref VA_AnimationMaterialPropertyData animationMaterialPropertyData,
                ref VA_AnimationLerpMaterialPropertyData lerpMaterialPropertyData,
                in VA_CurrentAnimationData animationData) => {
                    animationMaterialPropertyData = VA_AnimationMaterialPropertyData.Construct(
                        animationData.AnimationTime, animationData.AnimationIndex, 0, -1);
                    lerpMaterialPropertyData.Value = 0;
            }).ScheduleParallel(Dependency);
            
            Dependency = Entities
                .ForEach((ref VA_AnimationMaterialPropertyData animationMaterialPropertyData,
                    ref VA_AnimationLerpMaterialPropertyData lerpMaterialPropertyData,
                    in VA_CurrentAnimationData animationData, in VA_SmoothTransitionData smoothTransitionData) => {
                
                    var lerp = smoothTransitionData.TransitionTime <= 0
                        ? 1
                        : math.min(smoothTransitionData.ElapsedTransitionTime / smoothTransitionData.TransitionTime, 1);
                    animationMaterialPropertyData = VA_AnimationMaterialPropertyData.Construct(
                        animationData.AnimationTime, animationData.AnimationIndex, 
                        smoothTransitionData.NextAnimationTime, smoothTransitionData.NextAnimationIndex);
                    lerpMaterialPropertyData.Value = lerp;
                }).ScheduleParallel(Dependency);
        }
    }
}