using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;


namespace Awaken.TG.Graphics.DayNightSystem {
    [RequireComponent(typeof(VisualEffect))]
    public class DayNightSystemVisualEffectController : DayNightSystemComponentController {
        const string SpawnRateKey = "SpawnRate";
        const string ColorKey = "Color";
        const string ColorMultiplyKey = "ColorMultiply";
        
        [SerializeField, BoxGroup("Spawn Rate")] AnimationCurve spawnRate;
        
        [SerializeField, BoxGroup("Color"), ColorUsage(true, true)] Color dayColor;
        [SerializeField, BoxGroup("Color"), ColorUsage(true, true)] Color nightColor;
        [SerializeField, BoxGroup("Color")] AnimationCurve colorBlend;
        [SerializeField, BoxGroup("Color")] AnimationCurve colorMultiply;
        
        VisualEffect _visualEffect; 
        
        protected override void Init() {
            _visualEffect = GetComponent<VisualEffect>();
        }

        protected override void OnUpdate(float deltaTime) {
            if (_visualEffect != null) {
                float spawnRateValue = CalculateSpawnRateValue();
                float colorBlendValue = CalculateColorBlend().x;
                float colorMultiplyValue = CalculateColorBlend().y;

                if (_visualEffect.HasFloat(SpawnRateKey)) {
                    _visualEffect.SetFloat(SpawnRateKey, spawnRateValue);
                }

                if (_visualEffect.HasVector4(ColorKey)) {
                    Vector4 tint = CalculateColorAlphaValue(colorBlendValue);
                    _visualEffect.SetVector4(ColorKey, tint);
                    _visualEffect.SetFloat(ColorMultiplyKey, colorMultiplyValue);
                }
            }
        }

        float CalculateSpawnRateValue() {
            return spawnRate.Evaluate(TimeOfDay);
        }

        float2 CalculateColorBlend() {
            var tintValue = new float2(colorBlend.Evaluate(TimeOfDay), colorMultiply.Evaluate(TimeOfDay));
            return tintValue;
        }
        
        Vector4 CalculateColorAlphaValue(float blendFactor) {
            return Vector4.Lerp(dayColor, nightColor, blendFactor);
        }
    }
}
