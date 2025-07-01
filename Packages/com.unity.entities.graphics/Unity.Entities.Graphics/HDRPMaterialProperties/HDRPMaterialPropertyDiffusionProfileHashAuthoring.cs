#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_DiffusionProfileHash" )]
    public struct HDRPMaterialPropertyDiffusionProfileHash : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyDiffusionProfileHashAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyDiffusionProfileHash), nameof(HDRPMaterialPropertyDiffusionProfileHash.Value))]
        public float Value;

        class HDRPMaterialPropertyDiffusionProfileHashBaker : Baker<HDRPMaterialPropertyDiffusionProfileHashAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyDiffusionProfileHashAuthoring authoring)
            {
                HDRPMaterialPropertyDiffusionProfileHash component = default(HDRPMaterialPropertyDiffusionProfileHash);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
