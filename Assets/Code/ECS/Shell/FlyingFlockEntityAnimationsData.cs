using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [Serializable]
    public struct FlyingFlockEntityAnimationsData : IComponentData {
        public FlyingFlockEntityAnimationsData(byte flapAnimationIndex, byte soarAnimationIndex,
            byte restAnimationIndex, half2 flapSpeedMinMax,
            half transitionTime, bool useSoarLanding) {
            throw new NotImplementedException();
        }
    }
}