#if URP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_OcclusionStrength")]
    public struct URPMaterialPropertyOcclusionStrength : IComponentData
    {
        public float Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertyOcclusionStrengthAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertyOcclusionStrength), nameof(URPMaterialPropertyOcclusionStrength.Value))]
        public float Value;

        class URPMaterialPropertyOcclusionStrengthBaker : Unity.Entities.Baker<URPMaterialPropertyOcclusionStrengthAuthoring>
        {
            public override void Bake(URPMaterialPropertyOcclusionStrengthAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertyOcclusionStrength component = default(Unity.Rendering.URPMaterialPropertyOcclusionStrength);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
