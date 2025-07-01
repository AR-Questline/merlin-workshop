#if URP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Cutoff")]
    public struct URPMaterialPropertyCutoff : IComponentData
    {
        public float Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class URPMaterialPropertyCutoffAuthoring : UnityEngine.MonoBehaviour
    {
        [Unity.Entities.RegisterBinding(typeof(URPMaterialPropertyCutoff), nameof(URPMaterialPropertyCutoff.Value))]
        [UnityEngine.Range(0,1)]
        public float Value;

        class URPMaterialPropertyCutoffBaker : Unity.Entities.Baker<URPMaterialPropertyCutoffAuthoring>
        {
            public override void Bake(URPMaterialPropertyCutoffAuthoring authoring)
            {
                Unity.Rendering.URPMaterialPropertyCutoff component = default(Unity.Rendering.URPMaterialPropertyCutoff);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
