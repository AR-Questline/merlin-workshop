using System;
using System.Globalization;
using System.Linq;
using Awaken.TG.Graphics.VFX;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

namespace Awaken.TG.Graphics {
    [ExecuteInEditMode]
    public class WindController : MonoBehaviour {
        [BoxGroup("General"), Range(0, 100)] public float windSpeed = 20f;
        [BoxGroup("General"), Range(0, 360)] public float windDirection;

        [SerializeField, BoxGroup("AutoControl")] bool autoControl = true;
        [SerializeField, BoxGroup("AutoControl/Speed"), ReadOnly] float targetWindSpeed;
        [SerializeField, BoxGroup("AutoControl/Speed"), MinMaxSlider(0, 100, true)] Vector2 windSpeedRange = new Vector2(5f, 40f);
        [SerializeField, BoxGroup("AutoControl/Direction"), ReadOnly] float targetWindDirection;
        [SerializeField, BoxGroup("AutoControl/Direction")] float windDirectionAngle = 30f;
        [SerializeField, BoxGroup("AutoControl/Time"), MinMaxSlider(1, 60, true)] Vector2 windChangeInterval = new Vector2(10f, 30f);
        [SerializeField, BoxGroup("AutoControl/Time"), MinMaxSlider(1, 60, true)] Vector2 windChangeTime = new Vector2(30f, 60f);

        // --- Water ---
        [SerializeField, BoxGroup("Water")] bool affectOcean = true;
        [SerializeField, BoxGroup("Water/Ocean")] WaterSurface ocean;
        [SerializeField, BoxGroup("Water/Ocean")] float oceanWavesHeightMultiplier = 1;
        [SerializeField, BoxGroup("Water/Ocean")] AnimationCurve oceanRipplesIntensity = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );
        [SerializeField, BoxGroup("Water/Ocean")] AnimationCurve oceanFoamAmount = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );

        [SerializeField, BoxGroup("Water/Lakes")] bool affectLakes = true;
        [SerializeField, BoxGroup("Water/Lakes")] WaterSurface[] lakes = Array.Empty<WaterSurface>();
        [SerializeField, BoxGroup("Water/Lakes")] AnimationCurve lakesRipplesIntensity = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
            );
        [SerializeField, BoxGroup("Water/Lakes")] AnimationCurve lakesFoamAmount = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
            );

        // --- Skybox --- 
        [SerializeField, BoxGroup("Skybox")] bool affectSkybox = true;
        [SerializeField, BoxGroup("Skybox")] Volume[] volumesWithSky = Array.Empty<Volume>();
        [SerializeField, BoxGroup("Skybox")] float skyboxSpeedMultiplier = 5f;

        // --- Vegetation --- 
        [SerializeField, BoxGroup("Vegetation")] bool affectVegetation = true;
        
        // --- Local variables
        float _timer;
        float _currentInterval;

        VisualEnvironment[] _visualEnvironments = Array.Empty<VisualEnvironment>();
        
        void OnEnable() {
            EnsureSetup();
        }

        void OnValidate() {
            EnsureSetup();
        }

        void Update() {
            if (!LightController.EditorPreviewUpdates) return;

            DirectionFix();
            
            if (autoControl) {
                AutoControl();
            }

            if (affectOcean) {
                UpdateWater(ocean, oceanRipplesIntensity, oceanFoamAmount);
            }
            
            if (affectLakes) {
                foreach (var lake in lakes.WhereNotUnityNull()) {
                    UpdateWater(lake, lakesRipplesIntensity, lakesFoamAmount);
                }
            }
            
            if (affectSkybox) {
                SetSkybox();
            }
            
            if (affectVegetation) {
                SetVegetation();
            }
        }
        
        public void RegisterLake(WaterSurface lake) {
            if (lakes.Contains(lake)) return;
            
            Array.Resize(ref lakes, lakes.Length + 1);
            lakes[^1] = lake;
        }
        
        public void UnregisterLake(WaterSurface lake) {
            var index = Array.IndexOf(lakes, lake);
            if (index == -1) return;
            
            lakes[index] = lakes[^1];
            Array.Resize(ref lakes, lakes.Length - 1);
        }
        
        void EnsureSetup() {
            affectOcean = affectOcean && ocean;
            affectLakes = affectLakes && lakes.Length > 0;

            if (affectSkybox && volumesWithSky.Length > 0) {
                var count = volumesWithSky.Count(static v => v != null && v.GetSharedOrInstancedProfile().Has<VisualEnvironment>());
                _visualEnvironments = new VisualEnvironment[count];
                int i = 0;
                foreach (var timeOfDayVolume in volumesWithSky) {
                    if (timeOfDayVolume != null && timeOfDayVolume.GetSharedOrInstancedProfile().TryGet(out VisualEnvironment visualEnvironment)) {
                        _visualEnvironments[i++] = visualEnvironment;
                    }
                }
            }
            affectSkybox = affectSkybox && _visualEnvironments.Length > 0;
        }

        void DirectionFix() {
            if (windDirection < 0f) {
                windDirection += 360f;
            } else if (windDirection > 360f) {
                windDirection -= 360f;
            }
        }

        void AutoControl() {
            var deltaTime = Time.deltaTime;
            _timer += deltaTime;
            if (_timer >= _currentInterval) {
                targetWindSpeed = Random.Range(windSpeedRange.x, windSpeedRange.y);
                targetWindDirection = windDirection + Random.Range(-windDirectionAngle / 2f, windDirectionAngle / 2f);

                _timer = 0f;
                _currentInterval = Random.Range(windChangeInterval.x, windChangeInterval.y);
            }
            
            windSpeed = Mathf.Lerp(windSpeed, targetWindSpeed, deltaTime / Random.Range(windChangeTime.x, windChangeTime.y));
            windDirection = Mathf.Lerp(windDirection, targetWindDirection, deltaTime / Random.Range(windChangeTime.x, windChangeTime.y));
        }

        void SetVegetation() {
            var time = Time.time;
            
            Shader.SetGlobalFloat("_VegetationWindSpeed", windSpeed);
            Shader.SetGlobalFloat("_VegetationWindDirection", windDirection);
            Shader.SetGlobalFloat("_VegetationWindTime", time);
        }

        void SetSkybox() {
            var windSpeedValue = windSpeed * skyboxSpeedMultiplier;
            // Changed default skybox direction X to Z
            float rotatedWindDirection = windDirection - 90f;
            if (rotatedWindDirection < 0f) {
                rotatedWindDirection += 360f;
            } else if (rotatedWindDirection >= 360f) {
                rotatedWindDirection -= 360f;
            }
            var windOrientation = Mathf.Clamp(360-rotatedWindDirection, 0, 360);

            foreach (var visualEnvironment in _visualEnvironments) {
                visualEnvironment.windSpeed.value = windSpeedValue;
                visualEnvironment.windOrientation.value = windOrientation;
            }
        }

        void UpdateWater(WaterSurface water, AnimationCurve ripplesIntensity, AnimationCurve foamAmount) {
            water.largeWindSpeed = windSpeed * oceanWavesHeightMultiplier;
            water.largeOrientationValue = Mathf.Clamp(-(windDirection - 90f), -360, 360);
            water.ripplesWindSpeed = RemapCurve(windSpeed, 0f, 100f, 0f, 15f, ripplesIntensity);
            water.simulationFoamAmount = RemapCurve(windSpeed, 0f, 100f, 0f, 1f, foamAmount);
        }

        static float RemapCurve(float value, float oldMin, float oldMax, float newMin, float newMax, AnimationCurve curve) {
            if (value <= oldMin) {
                return newMin;
            }
            if (value >= oldMax) {
                return newMax;
            }

            float t = Mathf.InverseLerp(oldMin, oldMax, value);
            float curveValue = curve.Evaluate(t);
            return Mathf.Lerp(newMin, newMax, curveValue);
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            Color gizmoColor = Color.Lerp(Color.green, Color.red, windSpeed * 0.01f);

            // Wind speed label
            GUIStyle style = new GUIStyle();
            style.normal.textColor = gizmoColor;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            UnityEditor.Handles.Label(transform.position - new Vector3(0f, 0.5f, 0f), windSpeed.ToString("f2", CultureInfo.InvariantCulture), style);

            // Wind direction label
            UnityEditor.Handles.color = gizmoColor;
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.Euler(0f, windDirection, 0f), 2f, EventType.Repaint); // windDirection
            UnityEditor.Handles.DrawWireDisc(transform.position, transform.forward, 0.5f);

            if (autoControl) {
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.Euler(0f, targetWindDirection, 0f), 1f, EventType.Repaint); // windTarget
            }
        }
#endif
    }
}
