using System;
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

        public bool IsMoving {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool PrevIsMoving {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public CritterMovementState(float movementSpeed) {
            throw new NotImplementedException();
        }
    }
}