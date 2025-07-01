#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_AORemapMax"           )]
    public struct HDRPMaterialPropertyAORemapMax : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyAORemapMaxAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyAORemapMax), nameof(HDRPMaterialPropertyAORemapMax.Value))]
        public float Value;

        class HDRPMaterialPropertyAORemapMaxBaker : Baker<HDRPMaterialPropertyAORemapMaxAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyAORemapMaxAuthoring authoring)
            {
                HDRPMaterialPropertyAORemapMax component = default(HDRPMaterialPropertyAORemapMax);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
