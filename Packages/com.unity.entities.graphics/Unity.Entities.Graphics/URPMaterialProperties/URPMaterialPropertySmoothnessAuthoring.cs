#if URP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Smoothness")]
    public struct URPMaterialPropertySmoothness : IComponentData
    {
        public float Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertySmoothnessAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertySmoothness), nameof(URPMaterialPropertySmoothness.Value))]
        [UnityEngine.Range(0,1)]
        public float Value;

        class URPMaterialPropertySmoothnessBaker : Unity.Entities.Baker<URPMaterialPropertySmoothnessAuthoring>
        {
            public override void Bake(URPMaterialPropertySmoothnessAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertySmoothness component = default(Unity.Rendering.URPMaterialPropertySmoothness);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
