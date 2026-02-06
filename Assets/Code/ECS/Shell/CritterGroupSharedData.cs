using System;
using Unity.Entities;
using UnityEngine.Jobs;

namespace Awaken.ECS.Critters.Components {
    public struct CritterGroupSharedData : ISharedComponentData, IEquatable<CritterGroupSharedData> {
        public CritterMovementParams movementParams;
        public BlobAssetReference<CrittersPathPointsBlobData> pathPointsRef;
        public CritterSoundsGuids sounds;
        public TransformAccessArray transformsArray;

        public CritterGroupSharedData(CritterMovementParams movementParams,
            BlobAssetReference<CrittersPathPointsBlobData> pathPointsRef, CritterSoundsGuids sounds,
            TransformAccessArray transformsArray) {
            throw new NotImplementedException();
        }

        public bool Equals(CritterGroupSharedData other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }
    }
}