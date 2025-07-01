using Unity.Entities;

namespace Awaken.ECS.Components {
    public struct CullingDistancePreviewComponent : IComponentData {
        public float localDistance;
        public float localDistanceSq;
    }
}
