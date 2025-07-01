using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.IK {
    public struct ARFootIKData {
        public float3 footAnimationPosition;
        public quaternion footAnimationRotation;
        public float3 footDesiredOffset;
        public float3 raycastHitPosition;
        public float3 desiredFootNormal;
        public float desiredWeight;
        public float minIKHeightDifference;
        public float maxIKHeightDifference;
    }
    
    public struct ARSpineIKData {
        public quaternion spineAnimationRotation;
        public float weight;
    }
}