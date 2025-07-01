#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_ThicknessRemap"       )]
    public struct HDRPMaterialPropertyThicknessRemap : IComponentData { public float4 Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyThicknessRemapAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyThicknessRemap), nameof(HDRPMaterialPropertyThicknessRemap.Value))]
        public float4 Value;

        class HDRPMaterialPropertyThicknessRemapBaker : Baker<HDRPMaterialPropertyThicknessRemapAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyThicknessRemapAuthoring authoring)
            {
                HDRPMaterialPropertyThicknessRemap component = default(HDRPMaterialPropertyThicknessRemap);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
