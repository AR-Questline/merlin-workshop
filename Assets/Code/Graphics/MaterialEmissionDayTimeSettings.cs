using System;
using UnityEngine;

namespace Awaken.TG.Graphics {
    [Serializable]
    public class MaterialEmissionDayTimeSettings {
        public const string UseEmissionIntensity = "_UseEmissiveIntensity";
        public const string EmissionIntensity = "_EmissiveIntensity";

        public const string EmissionIntensityUnit = "_EmissiveIntensityUnit";
        public const int EmissionIntensityUnitNits = 0;
        public const int EmissionIntensityUnitEv100 = 1;
        public const string ExposureWeight = "_EmissiveExposureWeight";
        public const string EmissionColor = "_EmissiveColor";
        public const string EmissionColorLrd = "_EmissiveColorLDR";

        public bool useEv100IntensityUnits = true;
        public AnimationCurve intensityByDayTime = new AnimationCurve(new Keyframe(0, 10), new Keyframe(1, 10));
        public AnimationCurve exposureWeightByDayTime = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
        public Gradient colorByDayTime = new Gradient();

        public void ApplyToMaterial(in Material material, float timeOfDayNormalized) {
            try {
                var color = colorByDayTime.Evaluate(timeOfDayNormalized);
                var intensity = intensityByDayTime.Evaluate(timeOfDayNormalized);
                if (useEv100IntensityUnits) {
                    intensity = EV100ToNits(intensity);
                }

                material.SetFloat(UseEmissionIntensity, 1f);
                material.SetColor(EmissionColorLrd, color);
                material.SetColor(EmissionColor, color.linear * intensity);
                var exposureWeight = this.exposureWeightByDayTime.Evaluate(timeOfDayNormalized);
                material.SetFloat(ExposureWeight, exposureWeight);
#if UNITY_EDITOR
                //These parameters are used only in material inspector to manage EmissionColor value more conveniently. Setting values here to see the correct values in inspector
                material.SetFloat(EmissionIntensityUnit,
                    useEv100IntensityUnits ? EmissionIntensityUnitEv100 : EmissionIntensityUnitNits);
                material.SetFloat(EmissionIntensity, intensity);
#endif
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        static float EV100ToNits(float ev100) {
            return Mathf.Pow(2, ev100 - 3);
        }
    }
}