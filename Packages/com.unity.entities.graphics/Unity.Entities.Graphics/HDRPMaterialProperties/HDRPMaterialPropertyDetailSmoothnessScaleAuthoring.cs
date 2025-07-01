#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_DetailSmoothnessScale")]
    public struct HDRPMaterialPropertyDetailSmoothnessScale : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyDetailSmoothnessScaleAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyDetailSmoothnessScale), nameof(HDRPMaterialPropertyDetailSmoothnessScale.Value))]
        public float Value;

        class HDRPMaterialPropertyDetailSmoothnessScaleBaker : Baker<HDRPMaterialPropertyDetailSmoothnessScaleAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyDetailSmoothnessScaleAuthoring authoring)
            {
                HDRPMaterialPropertyDetailSmoothnessScale component = default(HDRPMaterialPropertyDetailSmoothnessScale);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
