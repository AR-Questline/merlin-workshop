#if URP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_SpecColor")]
    public struct URPMaterialPropertySpecColor : IComponentData
    {
        public float4 Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertySpecColorAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertySpecColor), nameof(URPMaterialPropertySpecColor.Value))]
        public UnityEngine.Color color;

        class URPMaterialPropertySpecColorBaker : Unity.Entities.Baker<URPMaterialPropertySpecColorAuthoring>
        {
            public override void Bake(URPMaterialPropertySpecColorAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertySpecColor component = default(Unity.Rendering.URPMaterialPropertySpecColor);
                float4 colorValues;
                colorValues.x = authoring.color.linear.r;
                colorValues.y = authoring.color.linear.g;
                colorValues.z = authoring.color.linear.b;
                colorValues.w = authoring.color.linear.a;
                component.Value = colorValues;

                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
