using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Elevator {
    public struct ElevatorChainData : IComponentData {
        public float platformCurrentPositionY;
        public float platformPrevPositionY;
        public float2 platformPositionXZ;
        public quaternion platformRotation;
        public float maxChainY;
        public float finalSingleChainHeight;
        public int spawnedChainLevelsCount;
        public NativeList<Entity> spawnedEntities;
        public NativeArray<float3> chainRootsPositions;

        public ElevatorChainData(float3 platformCurrentPosition, quaternion platformRotation,
            float maxChainY, float finalSingleChainHeight, NativeArray<float3> chainsRootsPositions,
            int entitiesPreallocateCount) {
            throw new NotImplementedException();
        }
    }
}