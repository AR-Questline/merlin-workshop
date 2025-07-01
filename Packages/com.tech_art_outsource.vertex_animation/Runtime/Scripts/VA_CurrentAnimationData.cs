using Unity.Entities;

namespace TAO.VertexAnimation {
    public struct VA_CurrentAnimationData : IComponentData {
        public float AnimationTime;
        public float AnimationSpeed;
        public float AnimationDuration;
        public byte AnimationIndex;

        public static readonly VA_CurrentAnimationData Default = new() {
            AnimationIndex = byte.MaxValue,
            AnimationTime = 0,
            AnimationSpeed = 0,
            AnimationDuration = 0
        };
    }
}