#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_SmoothnessRemapMax"   )]
    public struct HDRPMaterialPropertySmoothnessRemapMax : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertySmoothnessRemapMaxAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertySmoothnessRemapMax), nameof(HDRPMaterialPropertySmoothnessRemapMax.Value))]
        public float Value;

        class HDRPMaterialPropertySmoothnessRemapMaxBaker : Baker<HDRPMaterialPropertySmoothnessRemapMaxAuthoring>
        {
            public override void Bake(HDRPMaterialPropertySmoothnessRemapMaxAuthoring authoring)
            {
                HDRPMaterialPropertySmoothnessRemapMax component = default(HDRPMaterialPropertySmoothnessRemapMax);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
