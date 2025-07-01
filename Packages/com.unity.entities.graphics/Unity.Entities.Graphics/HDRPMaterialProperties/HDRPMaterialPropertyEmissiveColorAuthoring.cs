#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_EmissiveColor")]
    public struct HDRPMaterialPropertyEmissiveColor : IComponentData { public float3 Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyEmissiveColorAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyEmissiveColor), nameof(HDRPMaterialPropertyEmissiveColor.Value))]
        public float3 Value;

        class HDRPMaterialPropertyEmissiveColorBaker : Baker<HDRPMaterialPropertyEmissiveColorAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyEmissiveColorAuthoring authoring)
            {
                HDRPMaterialPropertyEmissiveColor component = default(HDRPMaterialPropertyEmissiveColor);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
