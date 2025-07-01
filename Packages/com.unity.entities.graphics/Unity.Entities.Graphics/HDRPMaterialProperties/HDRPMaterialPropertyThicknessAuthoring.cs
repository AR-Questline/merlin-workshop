#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Thickness"            )]
    public struct HDRPMaterialPropertyThickness : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyThicknessAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyThickness), nameof(HDRPMaterialPropertyThickness.Value))]
        public float Value;

        class HDRPMaterialPropertyThicknessBaker : Baker<HDRPMaterialPropertyThicknessAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyThicknessAuthoring authoring)
            {
                HDRPMaterialPropertyThickness component = default(HDRPMaterialPropertyThickness);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
