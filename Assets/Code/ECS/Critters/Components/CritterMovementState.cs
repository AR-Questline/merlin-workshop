using Awaken.Utility.LowLevel.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Critters.Components {
    public struct CritterMovementState : IComponentData {
        public float movementSpeed;
        public int currentPathPointIndex;
        public float idleWaitTimeRemaining;
        public float3 directionToNextPoint;
        public float currentPathSegmentLength;
        public quaternion alignmentRotationTowardNextPoint;
        public ByteBool8 isMovingStatuses;
        public bool IsMoving { get => isMovingStatuses.c0; set => isMovingStatuses.c0 = value; }
        public bool PrevIsMoving { get => isMovingStatuses.c1; set => isMovingStatuses.c1 = value; }

        public CritterMovementState(float movementSpeed) {
            this.movementSpeed = movementSpeed;
            currentPathPointIndex = 0;
            idleWaitTimeRemaining = 0;
            directionToNextPoint = default;
            currentPathSegmentLength = 0;
            alignmentRotationTowardNextPoint = default;
            isMovingStatuses = default;
            PrevIsMoving = true;
        }
    }
}