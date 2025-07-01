#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Smoothness"           )]
    public struct HDRPMaterialPropertySmoothness : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertySmoothnessAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertySmoothness), nameof(HDRPMaterialPropertySmoothness.Value))]
        public float Value;

        class HDRPMaterialPropertySmoothnessBaker : Baker<HDRPMaterialPropertySmoothnessAuthoring>
        {
            public override void Bake(HDRPMaterialPropertySmoothnessAuthoring authoring)
            {
                HDRPMaterialPropertySmoothness component = default(HDRPMaterialPropertySmoothness);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
