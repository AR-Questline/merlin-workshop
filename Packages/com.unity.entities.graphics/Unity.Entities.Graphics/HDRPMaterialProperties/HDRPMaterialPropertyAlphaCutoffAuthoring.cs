#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_AlphaCutoff"          )]
    public struct HDRPMaterialPropertyAlphaCutoff : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyAlphaCutoffAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyAlphaCutoff), nameof(HDRPMaterialPropertyAlphaCutoff.Value))]
        public float Value;

        class HDRPMaterialPropertyAlphaCutoffBaker : Baker<HDRPMaterialPropertyAlphaCutoffAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyAlphaCutoffAuthoring authoring)
            {
                var component = default(HDRPMaterialPropertyAlphaCutoff);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
