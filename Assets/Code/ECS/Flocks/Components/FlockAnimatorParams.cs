using TAO.VertexAnimation;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct FlockAnimatorParams : IComponentData {
        public VA_AnimatorParams value;
    }
}