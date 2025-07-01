#if URP_10_0_0_OR_NEWER
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    [MaterialProperty("_EmissionColor")]
    public struct URPMaterialPropertyEmissionColor : IComponentData
    {
        public float4 Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertyEmissionColorAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertyEmissionColor), nameof(URPMaterialPropertyEmissionColor.Value))]
        [ColorUsage(true, true)]
        public UnityEngine.Color color;

        class URPMaterialPropertyEmissionColorBaker : Unity.Entities.Baker<URPMaterialPropertyEmissionColorAuthoring>
        {
            public override void Bake(URPMaterialPropertyEmissionColorAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertyEmissionColor component = default(Unity.Rendering.URPMaterialPropertyEmissionColor);
                float4 colorValues;
                colorValues.x = authoring.color.r;
                colorValues.y = authoring.color.g;
                colorValues.z = authoring.color.b;
                colorValues.w = authoring.color.a;
                component.Value = colorValues;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
