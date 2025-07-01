#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_SpecularColor"        )]
    public struct HDRPMaterialPropertySpecularColor : IComponentData { public float4 Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertySpecularColorAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertySpecularColor), nameof(HDRPMaterialPropertySpecularColor.Value))]
        public float4 Value;

        class HDRPMaterialPropertySpecularColorBaker : Baker<HDRPMaterialPropertySpecularColorAuthoring>
        {
            public override void Bake(HDRPMaterialPropertySpecularColorAuthoring authoring)
            {
                HDRPMaterialPropertySpecularColor component = default(HDRPMaterialPropertySpecularColor);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
