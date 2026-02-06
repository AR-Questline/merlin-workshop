using System;
using TAO.VertexAnimation;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Critters {
    public struct CritterAnimatorParams : IComponentData {
        public CritterAnimatorParams(VA_AnimatorParams value) {
            throw new NotImplementedException();
        }

        public CritterAnimatorParams(half targetAnimationSpeed, half transitionTime, byte targetAnimationIndex) {
            throw new NotImplementedException();
        }
    }
}