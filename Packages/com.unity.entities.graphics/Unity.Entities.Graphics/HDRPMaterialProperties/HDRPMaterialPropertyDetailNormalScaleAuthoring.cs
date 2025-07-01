#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_DetailNormalScale"    )]
    public struct HDRPMaterialPropertyDetailNormalScale : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyDetailNormalScaleAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyDetailNormalScale), nameof(HDRPMaterialPropertyDetailNormalScale.Value))]
        public float Value;

        class HDRPMaterialPropertyDetailNormalScaleBaker : Baker<HDRPMaterialPropertyDetailNormalScaleAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyDetailNormalScaleAuthoring authoring)
            {
                HDRPMaterialPropertyDetailNormalScale component = default(HDRPMaterialPropertyDetailNormalScale);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
