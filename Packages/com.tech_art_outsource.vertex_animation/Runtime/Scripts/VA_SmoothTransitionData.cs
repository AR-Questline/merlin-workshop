using Unity.Entities;

namespace TAO.VertexAnimation {
    public struct VA_SmoothTransitionData : IComponentData {
        public byte NextAnimationIndex;
        public float NextAnimationTime;
        public float NextAnimationSpeed;
        public float NextAnimationDuration;
        public float ElapsedTransitionTime;
        public float TransitionTime;
    }
}