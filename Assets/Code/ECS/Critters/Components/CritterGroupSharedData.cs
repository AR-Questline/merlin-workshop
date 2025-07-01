using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Jobs;

namespace Awaken.ECS.Critters.Components {
    public struct CritterGroupSharedData : ISharedComponentData, IEquatable<CritterGroupSharedData> {
        public CritterMovementParams movementParams;
        public BlobAssetReference<CrittersPathPointsBlobData> pathPointsRef;
        public CritterSoundsGuids sounds;
        public TransformAccessArray transformsArray;
        public CritterGroupSharedData(CritterMovementParams movementParams, BlobAssetReference<CrittersPathPointsBlobData> pathPointsRef, CritterSoundsGuids sounds,
            TransformAccessArray transformsArray) {
            this.movementParams = movementParams;
            this.pathPointsRef = pathPointsRef;
            this.sounds = sounds;
            this.transformsArray = transformsArray;
        }

        public bool Equals(CritterGroupSharedData other) {
            return pathPointsRef.Equals(other.pathPointsRef);
        }

        public override bool Equals(object obj) {
            return obj is CritterGroupSharedData other && Equals(other);
        }

        public override int GetHashCode() {
            return pathPointsRef.GetHashCode();
        }
    }
}