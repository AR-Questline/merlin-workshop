#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_DetailAlbedoScale"    )]
    public struct HDRPMaterialPropertyDetailAlbedoScale : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyDetailAlbedoScaleAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyDetailAlbedoScale), nameof(HDRPMaterialPropertyDetailAlbedoScale.Value))]
        public float Value;

        class HDRPMaterialPropertyDetailAlbedoScaleBaker : Baker<HDRPMaterialPropertyDetailAlbedoScaleAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyDetailAlbedoScaleAuthoring authoring)
            {
                HDRPMaterialPropertyDetailAlbedoScale component = default(HDRPMaterialPropertyDetailAlbedoScale);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
