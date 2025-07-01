using System;
using Awaken.ECS.Critters;
using Awaken.ECS.Critters.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    [Serializable]
    public struct PathsArray {
        public CritterPathPointData[] Paths;
        public StartIndexAndLength[] PathsRanges;
        public int Count => IsValid ? PathsRanges.Length : 0;
        public bool IsValid => Paths != null && PathsRanges != null && Paths.Length > 0 && PathsRanges.Length > 0;

        public ArraySegment<CritterPathPointData> this[int index] => GetPathPoints(index);

        public ArraySegment<CritterPathPointData> GetPathPoints(int pathIndex) {
            var startIndexAndLength = PathsRanges[pathIndex];
            return new ArraySegment<CritterPathPointData>(Paths, startIndexAndLength.startIndex, startIndexAndLength.length);
        }

        [Serializable]
        public struct StartIndexAndLength {
            public int startIndex;
            public int length;

            public StartIndexAndLength(int startIndex, int length) {
                this.startIndex = startIndex;
                this.length = length;
            }
        }

        public BlobAssetReference<CrittersPathPointsBlobData> GetBlobAssetRef() => GetBlobAssetRef(this);
        
        public static BlobAssetReference<CrittersPathPointsBlobData> GetBlobAssetRef(PathsArray pathsArray) {
            using BlobBuilder blobBuilder = new(ARAlloc.Temp);
            ref CrittersPathPointsBlobData animationDataBlobAsset =
                ref blobBuilder.ConstructRoot<CrittersPathPointsBlobData>();

            var pathPoints = pathsArray.Paths;
            int pathPointsCount = pathPoints.Length;
            BlobBuilderArray<(float3 position, uint spheremapCompressedNormal)> pathPointsBlobArray = blobBuilder.Allocate(
                ref animationDataBlobAsset.pathPointsData, pathPointsCount);

            for (int i = 0; i < pathPointsCount; i++) {
                pathPointsBlobArray[i] = (pathPoints[i].position, CompressionUtils.EncodeNormalVectorSpheremap(pathPoints[i].Normal));
            }

            var pathsRanges = pathsArray.PathsRanges;
            var pathsCount = pathsRanges.Length;
            BlobBuilderArray<(int startIndex, int length)> pathsRangesBlobArray = blobBuilder.Allocate(
                ref animationDataBlobAsset.pathsRanges, pathsCount);

            for (int i = 0; i < pathsCount; i++) {
                pathsRangesBlobArray[i] = (pathsRanges[i].startIndex, pathsRanges[i].length);
            }

            return blobBuilder.CreateBlobAssetReference<CrittersPathPointsBlobData>(ARAlloc.Persistent);
        }
    }
}