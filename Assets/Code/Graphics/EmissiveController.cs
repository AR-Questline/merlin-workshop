using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class EmissiveController : MonoBehaviour {
        [HorizontalGroup("Box")]
        [SerializeField, BoxGroup("Box/Control"), LabelWidth(120)] 
        Gradient gradient = new Gradient();
        [SerializeField, BoxGroup("Box/Control"), LabelWidth(120)] 
        AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField, BoxGroup("Box/Control"), LabelWidth(120)] 
        float intensityMultiplier = 1.0f;
        [SerializeField, BoxGroup("Box/Control"), LabelWidth(120)] 
        float timeMultiplier = 4.0f;
        
        [SerializeField, BoxGroup("Box/Options"), LabelWidth(120)] 
        bool dayNightCycle = true;
        [SerializeField, BoxGroup("Box/Options"), LabelWidth(120)] 
        bool repeatable = true;
        [SerializeField, BoxGroup("Box/Options"), LabelWidth(120)] 
        bool global = true;
        
        static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
        static readonly int UseEmissiveIntensity = Shader.PropertyToID("_UseEmissiveIntensity");
        static readonly int EmissiveIntensityUnit = Shader.PropertyToID("_EmissiveIntensityUnit");
        static readonly int EmissiveIntensity = Shader.PropertyToID("_EmissiveIntensity");
        static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");
        static readonly int EmissiveColorLDR = Shader.PropertyToID("_EmissiveColorLDR");
        
        List<Material> _materials = new List<Material>();
        AnimationCurve _timeOfDayMultiplier = new AnimationCurve();
        float _startTime;
        float exposureWeight = 0.0f;
        float _timeOfDay;
        bool _canUpdate;
        
        void SetDayNightCycleCurve() {
            if (dayNightCycle) {
                Keyframe[] keyframes = new Keyframe[6];
                keyframes[0] = new Keyframe(0.0f, 1.0f);
                keyframes[1] = new Keyframe(0.2f, 1.0f);
                keyframes[2] = new Keyframe(0.4f, 0.0f);
                keyframes[3] = new Keyframe(0.6f, 0.0f);
                keyframes[4] = new Keyframe(0.8f, 1.0f);
                keyframes[5] = new Keyframe(1.0f, 1.0f);
                _timeOfDayMultiplier = new AnimationCurve(keyframes);
            }

            if (!dayNightCycle) {
                _timeOfDayMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
            }
        }
        
        void OnEnable() {
            _startTime = Time.time;
            _canUpdate = true;
        }

        void OnValidate() {
            SetDayNightCycleCurve();
        }

        void Start() {
            SetDayNightCycleCurve();
            
            if (global) {
                GetComponent<Renderer>().GetSharedMaterials(_materials);
            }
            else {
                GetComponent<Renderer>().GetMaterials(_materials);
            }
            _materials.ForEach(m => m.SetInt(UseEmissiveIntensity, 1));
            _materials.ForEach(m => m.SetInt(EmissiveIntensityUnit, 1));
        }
        void Update() {
            _timeOfDay = TimeOfDayPostProcessesController.dayNightCycle;
            if (_canUpdate) {
                var time = Time.time - _startTime;
            
                var colorToAssign = gradient.Evaluate(time / timeMultiplier);
                var intensityEval = intensityCurve.Evaluate(time / timeMultiplier);
                var timeOfDayMultiplierEval = _timeOfDayMultiplier.Evaluate(_timeOfDay);
                
                _materials.ForEach(m => UpdateEmissiveIntensity(m, colorToAssign, intensityEval * timeOfDayMultiplierEval * intensityMultiplier));
                _materials.ForEach(m => m.SetFloat(EmissiveExposureWeight, exposureWeight));
            
                if (!(time >= timeMultiplier)) return;
                if (repeatable) _startTime = Time.time;
            }
        }

        static void UpdateEmissiveIntensity(Material material, Color color, float intensity) {
            if (material.HasProperty(EmissiveColorLDR) && material.HasProperty(EmissiveIntensity) && material.HasProperty(EmissiveColor)) {
                material.SetColor(EmissiveColorLDR, color);
                material.SetColor(EmissiveColor, color * intensity);
                material.SetFloat(EmissiveIntensity, intensity);
            }
        }
    }
}
