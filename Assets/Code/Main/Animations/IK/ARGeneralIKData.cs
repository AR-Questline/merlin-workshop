using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.IK {
    public struct ARGeneralIKData {
        public bool isActive;
        public bool canMove;
        public float currentCharacterYPosition;
        public float hipsToRootOffset;
        public float3 forward;
        public float3 rootPosition;
        public quaternion rootLocalRotation;
        public float3 right;
        public float3 previousForward;
        public float3 slopeAvgNormal;
        public float rotationSpeed;
        public float deltaTime;
        public float spineRotationStrength;
        public float spineRotationSpeed;
    }
}