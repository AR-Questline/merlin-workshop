using Unity.Entities;

namespace TAO.VertexAnimation {
    public struct VA_AnimatorParams : IComponentData {
        public float targetAnimationSpeed;
        public float transitionTime;
        public byte targetAnimationIndex;
        public VA_AnimatorParams(float targetAnimationSpeed, float transitionTime, byte targetAnimationIndex) {
            this.targetAnimationSpeed = targetAnimationSpeed;
            this.transitionTime = transitionTime;
            this.targetAnimationIndex = targetAnimationIndex;
        }
    }
}