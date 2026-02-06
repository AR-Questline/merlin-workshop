using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Critters.Components {
    public struct CrittersPathPointsBlobData {
        public BlobArray<(float3 position, uint spheremapCompressedNormal)> pathPointsData;
        public BlobArray<(int startIndex, int length)> pathsRanges;
        public int PathsCount => pathsRanges.Length;
    }
}