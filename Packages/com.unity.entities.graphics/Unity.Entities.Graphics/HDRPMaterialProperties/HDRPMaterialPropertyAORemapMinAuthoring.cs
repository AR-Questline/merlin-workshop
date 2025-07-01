#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_AORemapMin"           )]
    public struct HDRPMaterialPropertyAORemapMin : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyAORemapMinAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyAORemapMin), nameof(HDRPMaterialPropertyAORemapMin.Value))]
        public float Value;

        class HDRPMaterialPropertyAORemapMinBaker : Baker<HDRPMaterialPropertyAORemapMinAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyAORemapMinAuthoring authoring)
            {
                HDRPMaterialPropertyAORemapMin component = default(HDRPMaterialPropertyAORemapMin);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
