using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.IK {
    public struct ARHeroSpineIKData {
        public quaternion spineAnimationRotation;
        public float weightX;
        public float weightY;
        public float weightZ;
        public SpineIKConstraint constraint;
        public float baseWeight;
    }
    
    public enum SpineIKConstraint {
        None = 0,
        BowAimOnly = 1,
        TwoHandedOnly = 2,
        OffHandSpearOrFist = 3,
    }
}