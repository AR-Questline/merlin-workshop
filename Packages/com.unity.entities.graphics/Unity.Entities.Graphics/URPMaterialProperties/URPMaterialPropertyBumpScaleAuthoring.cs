#if URP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_BumpScale")]
    public struct URPMaterialPropertyBumpScale : IComponentData
    {
        public float Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertyBumpScaleAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertyBumpScale), nameof(URPMaterialPropertyBumpScale.Value))]
        public float Value;

        class URPMaterialPropertyBumpScaleBaker : Unity.Entities.Baker<URPMaterialPropertyBumpScaleAuthoring>
        {
            public override void Bake(URPMaterialPropertyBumpScaleAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertyBumpScale component = default(Unity.Rendering.URPMaterialPropertyBumpScale);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
