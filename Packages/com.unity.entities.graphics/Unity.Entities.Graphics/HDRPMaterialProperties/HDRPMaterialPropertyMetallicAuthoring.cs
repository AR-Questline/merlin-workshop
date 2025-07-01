#if HDRP_10_0_0_OR_NEWER
using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Metallic"             )]
    public struct HDRPMaterialPropertyMetallic : IComponentData { public float  Value; }

    [UnityEngine.DisallowMultipleComponent]
    public class HDRPMaterialPropertyMetallicAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(typeof(HDRPMaterialPropertyMetallic), nameof(HDRPMaterialPropertyMetallic.Value))]
        public float Value;

        class HDRPMaterialPropertyMetallicBaker : Baker<HDRPMaterialPropertyMetallicAuthoring>
        {
            public override void Bake(HDRPMaterialPropertyMetallicAuthoring authoring)
            {
                HDRPMaterialPropertyMetallic component = default(HDRPMaterialPropertyMetallic);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
#endif
