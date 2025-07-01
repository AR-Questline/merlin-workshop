#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_UnlitColor"           )]
    public struct HDRPMaterialPropertyUnlitColor : IComponentData { public float4 Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyUnlitColorAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyUnlitColor), nameof(HDRPMaterialPropertyUnlitColor.Value))]
        public float4 Value;

        class HDRPMaterialPropertyUnlitColorBaker : Baker<HDRPMaterialPropertyUnlitColorAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyUnlitColorAuthoring authoring)
            {
                HDRPMaterialPropertyUnlitColor component = default(HDRPMaterialPropertyUnlitColor);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
