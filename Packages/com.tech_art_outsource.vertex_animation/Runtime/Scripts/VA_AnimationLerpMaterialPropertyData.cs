using Unity.Entities;
using Unity.Rendering;

namespace TAO.VertexAnimation {
    [MaterialProperty("_AnimationsLerp")]
    public struct VA_AnimationLerpMaterialPropertyData : IComponentData
    {
        //AnimationWeight, AnimationNextWeight, LerpValue,
        public float Value;
    }
}