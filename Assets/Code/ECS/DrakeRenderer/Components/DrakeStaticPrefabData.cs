using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.DrakeRenderer.Components {
    [InternalBufferCapacity(1)]
    public struct DrakeStaticPrefabData : IBufferElementData {
        public DrakeMeshMaterialComponent drakeMeshMaterial;
        public float3 worldBoundsCenterOffset;
        public float3 worldBoundsExtents;
        public float3 lodWorldReferencePointOffset;
    }
}