#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_SmoothnessRemapMin"   )]
    public struct HDRPMaterialPropertySmoothnessRemapMin : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertySmoothnessRemapMinAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertySmoothnessRemapMin), nameof(HDRPMaterialPropertySmoothnessRemapMin.Value))]
        public float Value;

        class HDRPMaterialPropertySmoothnessRemapMinBaker : Baker<HDRPMaterialPropertySmoothnessRemapMinAuthoring>
        {
            public override void Bake(HDRPMaterialPropertySmoothnessRemapMinAuthoring authoring)
            {
                HDRPMaterialPropertySmoothnessRemapMin component = default(HDRPMaterialPropertySmoothnessRemapMin);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
