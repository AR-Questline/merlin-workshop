using System;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Graphics.VFX {
    public class LightsOverrideArea : LightsOverride {
        [SerializeField] float areaOfEffectRadius = 50;
        [SerializeField] float blendingDistance = 10;
        [SerializeField, Required] Transform staticAreaCenterTransform;
        [SerializeField] bool overrideSkyTint;
        [SerializeField, Indent, ShowIf(nameof(overrideSkyTint)), Required] HdriSkyBlender skyBlender;
        [SerializeField, Indent, ShowIf(nameof(overrideSkyTint)), LabelText("Sky Tint")] Color skyTintOverride = Color.white;
        
        Vector3 _areaCenter;
        [NonSerialized]
        byte? _tintAddedAtPriority;
        
        protected override bool RunUpdateInEditMode => true;
        protected override void Setup() {
            _areaCenter = staticAreaCenterTransform.position;
            base.Setup();
        }

        public override void OnLateUpdate(float deltaTime) {
            if (TryGetHeroPos(out var pos)) {
                RunUpdate(pos, deltaTime);
            }
        }

        void RunUpdate(Vector3 pos, float deltaTime) {
            var distanceSqToCenter = (pos - _areaCenter).sqrMagnitude;
            if (distanceSqToCenter < math.square(areaOfEffectRadius)) {
                // not using distanceSquared because calculating distanceBlendingFactor from distanceSquared (and therefore other variables squared) results in non-linear interpolation  
                var distanceToCenter = math.sqrt(distanceSqToCenter);
                var blendingEndDistance = (math.max(areaOfEffectRadius - blendingDistance, 0));
                var distanceBlendingFactor = math.min(math.unlerp(areaOfEffectRadius, blendingEndDistance, distanceToCenter), 1);
                ApplyLightOverrides(deltaTime, distanceBlendingFactor);
                
                if (overrideSkyTint) {
                    ApplyTintOverride(distanceBlendingFactor);
                }
            } else {
                StopOverride();
                if (overrideSkyTint) {
                    ApplyTintOverride(0);
                }
            }
        }
        void ApplyTintOverride(float distanceBlendingFactor) {
            var disableOverride = distanceBlendingFactor <= 0;
            if (disableOverride) {
                if (_tintAddedAtPriority.HasValue == false) {
                    return;
                }
                skyBlender.StopTintOverride(priority);
                _tintAddedAtPriority = null;
                return;
            }

            if (_tintAddedAtPriority == null) {
                skyBlender.StartTintOverride(priority);
                _tintAddedAtPriority = priority;
            }

            Color newColor = Color.Lerp(skyBlender.TintWithLowerPriority(priority), skyTintOverride, distanceBlendingFactor);
            skyBlender.SetTintOverride(newColor, priority);
        }

        bool TryGetHeroPos(out Vector3 pos) {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                var lastActiveSceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (lastActiveSceneView != null && lastActiveSceneView.camera != null) {
                    pos = lastActiveSceneView.camera.transform.position;
                } else {
                    pos = default;
                    return false;
                }
            } else
#endif
            {
                if (Hero.Current == null) {
#if UNITY_EDITOR
                    if (false)
#endif
                    {
                        // In the editor, starting from a scene containing this component may run update before Hero.Current is initialized
                        MVC.UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
                    }
                    pos = default;
                    return false;
                }
                pos = Hero.Current.Coords;
            }
            return true;
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
            if (_tintAddedAtPriority.HasValue) {
                skyBlender.StopTintOverride(_tintAddedAtPriority.Value);
                _tintAddedAtPriority = null;
            }
        }

        void OnDrawGizmos() {
            if (staticAreaCenterTransform != null) {
                Gizmos.color = new Color(0, 0.5f, 1);
                Gizmos.DrawWireSphere(staticAreaCenterTransform.position, areaOfEffectRadius);
                
                Gizmos.color = new Color(0, 0.8f, 0.5f);
                Gizmos.DrawWireSphere(staticAreaCenterTransform.position, areaOfEffectRadius - blendingDistance);
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
                if (data.volumetricDimmerOverride.useOverride == false) {
                    data.volumetricDimmerOverride.volumetricDimmer = data.light.volumetricDimmer;
                }
                if (data.intensityOverride.useOverride == false) {
                    data.intensityOverride.intensity = data.light.intensity;
                }
                lightsWithOverrides[i] = data;
            }
        }
#endif
    }
}