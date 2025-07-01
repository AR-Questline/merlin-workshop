using Unity.Entities;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    [MaterialProperty("unity_SHCoefficients")]
    internal struct BuiltinMaterialPropertyUnity_SHCoefficients : IComponentData
    {
        public SHCoefficients Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    internal class BuiltinMaterialPropertyUnity_SHCoefficientsAuthoring : UnityEngine.MonoBehaviour
    {
        [RegisterBinding(nameof(SHCoefficients.SHAr), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHAr))]
        [RegisterBinding(nameof(SHCoefficients.SHAg), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHAg))]
        [RegisterBinding(nameof(SHCoefficients.SHAb), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHAb))]
        [RegisterBinding(nameof(SHCoefficients.SHBr), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHBr))]
        [RegisterBinding(nameof(SHCoefficients.SHBg), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHBg))]
        [RegisterBinding(nameof(SHCoefficients.SHBb), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHBb))]
        [RegisterBinding(nameof(SHCoefficients.SHC), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.SHC))]
        [RegisterBinding(nameof(SHCoefficients.ProbesOcclusion), typeof(BuiltinMaterialPropertyUnity_SHCoefficients), nameof(BuiltinMaterialPropertyUnity_SHCoefficients.Value) + "." + nameof(SHCoefficients.ProbesOcclusion))]
        public SHCoefficients Value;

        class BuiltinMaterialPropertyUnity_SHCoefficientsBaker : Baker<BuiltinMaterialPropertyUnity_SHCoefficientsAuthoring>
        {
            public override void Bake(BuiltinMaterialPropertyUnity_SHCoefficientsAuthoring authoring)
            {
                BuiltinMaterialPropertyUnity_SHCoefficients component = default(BuiltinMaterialPropertyUnity_SHCoefficients);
                component.Value = authoring.Value;
                // This test might require transform components
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
