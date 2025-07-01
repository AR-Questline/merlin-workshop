using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.VFX {
    public class LightOverrideArea : MonoBehaviour, UnityUpdateProvider.IWithLateUpdateGeneric {
        [SerializeField, Required] LightWithOverrides[] lightsWithOverrides = Array.Empty<LightWithOverrides>();
        [SerializeField, Required] Transform staticAreaCenterTransform;
        [SerializeField] byte priority = 0;
        [SerializeField] float areaOfEffectRadius = 50;
        [SerializeField] float blendingDistance = 10;
        [SerializeField] float intensityMultiplierLerpSpeed = 1f;
        Vector3 _areaCenter;
        bool _startedOverride;
        GameRealTime _gameRealTime;

        void OnEnable() {
            Setup();
            UnityUpdateProvider.GetOrCreate().RegisterLateGeneric(this);
        }

        void OnDisable() {
            StopOverride();
            UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
        }

        public void UnityLateUpdate(float deltaTime) {
            if (Hero.Current == null) {
                return;
            }
            var heroPos = Hero.Current.Coords;
            var heroToCenterDistanceSq = (heroPos - _areaCenter).sqrMagnitude;
            var areaRadiusSq = math.square(areaOfEffectRadius);
            if (heroToCenterDistanceSq < areaRadiusSq) {
                var heroToCenterDistance = math.sqrt(heroToCenterDistanceSq);
                var blendingStartDistance = areaOfEffectRadius;
                var blendingEndDistance = math.max(areaOfEffectRadius - blendingDistance, 0);
                var distanceBlendingFactor = math.min(math.unlerp(blendingStartDistance, blendingEndDistance, heroToCenterDistance), 1);
                int lightsCount = lightsWithOverrides.Length;
                for (int i = 0; i < lightsCount; i++) {
                    var data = lightsWithOverrides[i];
                    if (data.colorOverride.useOverride) {
                        data.light.SetColorOverride(Color.Lerp(data.light.color, data.colorOverride.color, distanceBlendingFactor), priority);
                    }
                    if (data.colorTemperatureOverride.useOverride) {
                        data.light.SetColorTemperatureOverride(math.lerp(data.light.colorTemperature, data.colorTemperatureOverride.colorTemperature, distanceBlendingFactor), priority);
                    }
                    bool isMatchingDaytime = false;
                    if (data.daytimeIntensityMultiplier.useOverride) {
                        isMatchingDaytime = data.daytimeIntensityMultiplier.applyTime == ApplyTime.Always ||
                                            (_gameRealTime != null &&
                                             ((_gameRealTime.WeatherTime.IsDay && data.daytimeIntensityMultiplier.applyTime == ApplyTime.Day) ||
                                              (_gameRealTime.WeatherTime.IsDay == false && data.daytimeIntensityMultiplier.applyTime == ApplyTime.Night)));
                    }
                    float fromIntensity = 1;
                    float toIntensity = 2;
                    var currentNotOverridenIntensity = data.light.intensity;
                    if (data.intensityOverride.useOverride) {
                        if (data.daytimeIntensityMultiplier.useOverride) {
                            if (data.daytimeIntensityMultiplier.applyOverrideOnlyAtThisDaytime && isMatchingDaytime == false) {
                                fromIntensity = data.intensityOverride.nativeIntensityWithDaytimeMultiplier;
                                toIntensity = currentNotOverridenIntensity;
                            } else {
                                fromIntensity = currentNotOverridenIntensity;
                                toIntensity = isMatchingDaytime ? data.intensityOverride.nativeIntensityWithDaytimeMultiplier : data.intensityOverride.nativeIntensity;
                            }
                        } else {
                            fromIntensity = currentNotOverridenIntensity;
                            toIntensity = data.intensityOverride.nativeIntensity;
                        }
                    } else if (data.daytimeIntensityMultiplier.useOverride) {
                        if (isMatchingDaytime) {
                            fromIntensity = currentNotOverridenIntensity;
                            // Only EV100 is non-linear light units, but none of lights have EV100 as native intensity, so it is safe and correct to 
                            // multiply light.intensity by multiplier and use the result as is.
                            toIntensity = currentNotOverridenIntensity * data.daytimeIntensityMultiplier.intensityMultiplier;
                        } else {
                            // If daytime multiplier was used but now time is not matching with applyTime
                            fromIntensity = currentNotOverridenIntensity * data.daytimeIntensityMultiplier.intensityMultiplier;
                            toIntensity = currentNotOverridenIntensity;
                        }
                    }
                    if (data.daytimeIntensityMultiplier.useOverride || data.intensityOverride.useOverride) {
                        var currentOverridenIntensityWithoutDistanceBlending = distanceBlendingFactor != 0 ?
                            mathExt.FindLerpEndValue(currentNotOverridenIntensity, distanceBlendingFactor, data.light.IntensityWithOverride) :
                            currentNotOverridenIntensity;
                        var tValue = math.unlerp(fromIntensity, toIntensity, currentOverridenIntensityWithoutDistanceBlending);
                        tValue = math.clamp(tValue + (intensityMultiplierLerpSpeed * deltaTime), 0, 1);
                        var thisFrameOverrideIntensity = math.lerp(fromIntensity, toIntensity, tValue);
                        data.light.SetIntensityOverride(math.lerp(currentNotOverridenIntensity, thisFrameOverrideIntensity, distanceBlendingFactor), priority);
                    }
                }
                if (!_startedOverride) {
                    _startedOverride = true;
                    _gameRealTime = World.Any<GameRealTime>();
                    for (int i = 0; i < lightsCount; i++) {
                        var data = lightsWithOverrides[i];
                        if (data.colorOverride.useOverride) {
                            data.light.StartColorOverride();
                        }
                        if (data.colorTemperatureOverride.useOverride) {
                            data.light.StartColorTemperatureOverride();
                        }
                        if (data.daytimeIntensityMultiplier.useOverride || data.intensityOverride.useOverride) {
                            data.light.StartIntensityOverride();
                        }
                    }
                }
            } else {
                StopOverride();
            }
        }

        void StopOverride() {
            if (!_startedOverride) {
                return;
            }
            _startedOverride = false;
            int lightsCount = lightsWithOverrides.Length;
            for (int i = 0; i < lightsCount; i++) {
                var data = lightsWithOverrides[i];
                if (data.colorOverride.useOverride) {
                    data.light.StopColorOverride();
                }
                if (data.colorTemperatureOverride.useOverride) {
                    data.light.StopColorTemperatureOverride();
                }
                if (data.intensityOverride.useOverride || data.daytimeIntensityMultiplier.useOverride) {
                    data.light.StopIntensityOverride();
                }
            }
        }

        void Setup() {
            _areaCenter = staticAreaCenterTransform.transform.position;
            for (int i = 0; i < lightsWithOverrides.Length; i++) {
                var data = lightsWithOverrides[i];
                if (data.intensityOverride.useOverride) {
                    try {
                        data.intensityOverride.nativeIntensity = LightUnitUtils.ConvertIntensity(
                            data.light.Light, data.intensityOverride.intensity, data.intensityOverride.lightUnit, LightUnitUtils.GetNativeLightUnit(data.light.Light.type));
                        float daytimeIntensityMultiplier = data.daytimeIntensityMultiplier.useOverride ? data.daytimeIntensityMultiplier.intensityMultiplier : 1;
                        data.intensityOverride.nativeIntensityWithDaytimeMultiplier = LightUnitUtils.ConvertIntensity(
                            data.light.Light, data.intensityOverride.intensity * daytimeIntensityMultiplier, data.intensityOverride.lightUnit, LightUnitUtils.GetNativeLightUnit(data.light.Light.type));
                        lightsWithOverrides[i] = data;
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                } else {
                    if (data.light.Light != null) {
                        data.intensityOverride.lightUnit = data.light.Light.lightUnit;
                        lightsWithOverrides[i] = data;
                    }
                }
            }
            _gameRealTime = World.Any<GameRealTime>();
        }

#if UNITY_EDITOR
        void Reset() {
            staticAreaCenterTransform = transform;
            SetDefaultOverrideValues();
        }

        void OnValidate() {
            if (Application.isPlaying) {
                return;
            }
            if (blendingDistance > areaOfEffectRadius) {
                Log.Debug?.Warning($"{nameof(blendingDistance)} cannot be larger than {areaOfEffectRadius}");
                blendingDistance = areaOfEffectRadius;
            }
            Setup();
            SetDefaultOverrideValues();
        }

        void OnDrawGizmos() {
            if (staticAreaCenterTransform != null) {
                Gizmos.color = new Color(0, 0.5f, 1);
                Gizmos.DrawWireSphere(staticAreaCenterTransform.position, areaOfEffectRadius);
            }
        }

        void SetDefaultOverrideValues() {
            for (int i = 0; i < lightsWithOverrides.Length; i++) {
                var data = lightsWithOverrides[i];
                if (data.light == null) {
                    continue;
                }
                if (data.colorOverride.useOverride == false) {
                    data.colorOverride.color = data.light.color;
                }
                if (data.colorTemperatureOverride.useOverride == false) {
                    data.colorTemperatureOverride.colorTemperature = data.light.colorTemperature;
                }
                if (data.intensityOverride.useOverride == false) {
                    data.intensityOverride.intensity = data.light.intensity;
                }
                lightsWithOverrides[i] = data;
            }
        }
#endif

        [Serializable]
        struct LightWithOverrides {
            public LightWithOverride light;

            [LabelText("filter"), Toggle(nameof(ColorOverride.useOverride))]
            public ColorOverride colorOverride;

            [LabelText("temperature"), Toggle(nameof(TemperatureOverride.useOverride))]
            public TemperatureOverride colorTemperatureOverride;

            [SerializeField, LabelText("intensity"), Toggle(nameof(IntensityOverride.useOverride))]
            public IntensityOverride intensityOverride;

            [SerializeField, LabelText("daytime intensity multiplier"), Toggle(nameof(IntensityOverride.useOverride))]
            public DaytimeIntensityMultiplierOverride daytimeIntensityMultiplier;
        }

        [Serializable]
        struct ColorOverride {
            public bool useOverride;
            public Color color;
        }

        [Serializable]
        struct TemperatureOverride {
            public bool useOverride;
            public float colorTemperature;
        }

        [Serializable]
        struct IntensityOverride {
            public bool useOverride;
            public LightUnit lightUnit;
            public float intensity;
            [HideInInspector] public float nativeIntensity;
            [HideInInspector] public float nativeIntensityWithDaytimeMultiplier;
        }

        [Serializable]
        struct DaytimeIntensityMultiplierOverride {
            public bool useOverride;
            public ApplyTime applyTime;
            public bool applyOverrideOnlyAtThisDaytime;
            public float intensityMultiplier;
        }

        public enum ApplyTime : byte {
            Always = 0,
            Day = 1,
            Night = 2
        }
    }
}