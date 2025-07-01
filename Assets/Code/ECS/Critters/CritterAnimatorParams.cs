using TAO.VertexAnimation;
using Unity.Entities;

namespace Awaken.ECS.Critters {
    public struct CritterAnimatorParams : IComponentData {
        public VA_AnimatorParams value;
        public CritterAnimatorParams(VA_AnimatorParams value) {
            this.value = value;
        }
        
        public CritterAnimatorParams(float targetAnimationSpeed, float transitionTime, byte targetAnimationIndex) {
            value = new VA_AnimatorParams(targetAnimationSpeed, transitionTime, targetAnimationIndex);
        } 
    }
}