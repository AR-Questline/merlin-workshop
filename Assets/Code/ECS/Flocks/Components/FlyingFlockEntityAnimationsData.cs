using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    [Serializable]
    public struct FlyingFlockEntityAnimationsData : IComponentData {
        public byte flapAnimationIndex;
        public byte soarAnimationIndex;
        public byte restAnimationIndex;

        [Space(5)]
        public float2 flapSpeedMinMax;
        public float transitionTime;
        [Tooltip("If true, flock entity will soar just before landing on the rest spot")]
        public bool useSoarLanding;

        public FlyingFlockEntityAnimationsData(byte flapAnimationIndex, byte soarAnimationIndex, byte restAnimationIndex, float2 flapSpeedMinMax, float transitionTime, bool useSoarLanding) {
            this.flapAnimationIndex = flapAnimationIndex;
            this.soarAnimationIndex = soarAnimationIndex;
            this.restAnimationIndex = restAnimationIndex;
            this.flapSpeedMinMax = flapSpeedMinMax;
            this.transitionTime = transitionTime;
            this.useSoarLanding = useSoarLanding;
        }
    }
}