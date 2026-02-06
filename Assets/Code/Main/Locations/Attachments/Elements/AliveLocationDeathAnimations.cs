using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class AliveLocationDeathAnimations : Element<Location>, IRefreshedByAttachment<AliveLocationDeathAnimationsAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveLocationDeathAnimations;

        AliveLocationDeathAnimationsAttachment _spec;
        
        public void InitFromAttachment(AliveLocationDeathAnimationsAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
        }
        
        void OnBeforeDeath(DamageOutcome _) {
            var transform = ParentModel.MainView.transform;
            if (_spec.animateDrakeMaterial) {
                foreach (var controller in transform.GetComponentsInChildren<DrakeAnimatedPropertiesOverrideController>()) {
                    if (_spec.reverseDirection) {
                        controller.StartBackward();
                    } else {
                        controller.StartForward();
                    }
                }
            }

            if (_spec.modifyVFXes || _spec.stopLights) {
                if (_spec.stopDuration > 0) {
                    StopEffectsAsync(_spec.modifyVFXes, _spec.stopLights, _spec.stopDuration).Forget();
                } else {
                    StopEffects(_spec.modifyVFXes, _spec.stopLights);
                }
            }
        }

        void StopEffects(bool animateVFX, bool animateLights) {
            var transform = ParentModel.MainView.transform;

            if (animateVFX) {
                foreach (var vfx in transform.GetComponentsInChildren<VisualEffect>()) {
                    if (_spec.stopVFXes) {
                        vfx.Stop();
                    }

                    foreach (var propertyData in _spec.vfxProperties) {
                        if (vfx.HasFloat(propertyData.propertyName)) {
                            vfx.SetFloat(propertyData.propertyName, propertyData.value);
                        }
                    }
                }
            }

            if (animateLights) {
                foreach (var light in transform.GetComponentsInChildren<HDAdditionalLightData>()) {
                    light.lightDimmer = 0f;
                    light.volumetricDimmer = 0f;
                }
            }
        }

        async UniTaskVoid StopEffectsAsync(bool animateVFX, bool animateLights, float duration) {
            var transform = ParentModel.MainView.transform;
            
            VisualEffect[] vfxs;
            float[][] vfxStartingValues;
            if (animateVFX) {
                vfxs = transform.GetComponentsInChildren<VisualEffect>(true);
                if (vfxs.Length > 0) {
                    if (_spec.vfxProperties.Length > 0) {
                        vfxStartingValues = new float[vfxs.Length][];
                    } else {
                        vfxStartingValues = null;
                    }
                    
                    for (int i = 0; i < vfxs.Length; i++) {
                        if (_spec.stopVFXes) {
                            vfxs[i].Stop();
                        }

                        if (_spec.vfxProperties.Length > 0) {
                            vfxStartingValues![i] = new float[_spec.vfxProperties.Length];
                            for (int j = 0; j < _spec.vfxProperties.Length; j++) {
                                if (vfxs[i].HasFloat(_spec.vfxProperties[j].propertyName)) {
                                    vfxStartingValues[i][j] = vfxs[i].GetFloat(_spec.vfxProperties[j].propertyName);
                                } else {
                                    vfxStartingValues[i][j] = float.NaN;
                                }
                            }
                        }
                    }
                } else {
                    vfxStartingValues = null;
                }
            } else {
                vfxs = null;
                vfxStartingValues = null;
            }

            HDAdditionalLightData[] lights;
            float[][] lightStartingIntensities;
            if (animateLights) {
                lights = transform.GetComponentsInChildren<HDAdditionalLightData>(true);
                if (lights.Length > 0) {
                    lightStartingIntensities = new float[lights.Length][];
                    for (int i = 0; i < lightStartingIntensities.Length; i++) {
                        lightStartingIntensities[i] = new float[2];
                        lightStartingIntensities[i][0] = lights[i].lightDimmer;
                        lightStartingIntensities[i][1] = lights[i].volumetricDimmer;
                    }
                } else {
                    lightStartingIntensities = null;
                }
            } else {
                lights = null;
                lightStartingIntensities = null;
            }

            float remainingTime = duration;
            while (remainingTime > 0f) {
                remainingTime -= Time.deltaTime;
                if (remainingTime < 0f) {
                    remainingTime = 0f;
                }
                var percentage = remainingTime / duration;
                SetValues(percentage);
                if (!await AsyncUtil.DelayFrame(this)) {
                    return;
                }
            }
            SetValues(0f);
            return;

            void SetValues(float percentage) {
                if (vfxStartingValues != null) {
                    for (int i = 0; i < vfxStartingValues.Length; i++) {
                        for (int j = 0; j < vfxStartingValues[i].Length; j++) {
                            if (!float.IsNaN(vfxStartingValues[i][j])) {
                                float value = math.lerp(_spec.vfxProperties[j].value, vfxStartingValues[i][j], percentage);
                                vfxs![i].SetFloat(_spec.vfxProperties[j].propertyName, value);
                            }
                        }
                    }
                }
                if (lightStartingIntensities != null) {
                    for (int i = 0; i < lightStartingIntensities.Length; i++) {
                        lights![i].lightDimmer = lightStartingIntensities[i][0] * percentage;
                        lights![i].volumetricDimmer = lightStartingIntensities[i][1] * percentage;
                    }
                }
            }
        }
    }
}