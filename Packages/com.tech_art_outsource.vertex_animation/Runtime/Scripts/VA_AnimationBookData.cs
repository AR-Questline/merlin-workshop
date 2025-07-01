using Unity.Entities;
using Unity.Collections;

namespace TAO.VertexAnimation {
    [System.Serializable]
    public struct VA_AnimationData {
#if UNITY_EDITOR
        // The name of the animation.
        public FixedString64Bytes EDITOR_name;
#endif
        // Total time of the animation.
        public float duration;
        public VA_AnimationData(float duration
#if UNITY_EDITOR
            , FixedString64Bytes name
#endif
            ) {
            this.duration = duration;
#if UNITY_EDITOR
            this.EDITOR_name = name;
#endif
        }
    }

    public struct VA_AnimationBookData {
        public BlobArray<VA_AnimationData> animationsDatas;
    }
}