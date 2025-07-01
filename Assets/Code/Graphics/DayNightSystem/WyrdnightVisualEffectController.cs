using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class WyrdnightVisualEffectController : WyrdNightControllerBase {
        [SerializeField] VisualEffect visualEffect;
        [SerializeField] string floatParameterName = "_Spawn";
        [SerializeField, LabelText("Disable On OnCurve End Value")] bool disableOnZero = true;
        [SerializeField, LabelText("Disable On OffCurve End Value")] bool disableOnEndValue = false;

        WyrdnightSplineRepeller _repeller;
        int _floatParameterId;
        float _cachedStartingValue, _cachedEndingValue;
        
        protected override void OnAwake() {
            if (visualEffect == null) {
                Debug.LogError("No VisualEffect component found on " + gameObject.HierarchyPath());
                enabled = false;
                return;
            }
            _cachedStartingValue = EnabledValue;
            _cachedEndingValue = DisabledValue;
            _floatParameterId = Shader.PropertyToID(floatParameterName);
            ApplyEffect(DisabledValue);
            _repeller = GetComponentInParent<WyrdnightSplineRepeller>();
        }

        protected override void ApplyEffect(float value) {
            visualEffect.SetFloat(_floatParameterId, value);
            if (disableOnZero || disableOnEndValue) {
                bool visualEffectEnabled = (!disableOnZero || !Mathf.Approximately(value, _cachedStartingValue)) 
                                           && (!disableOnEndValue || !Mathf.Approximately(value, _cachedEndingValue));
                visualEffect.enabled = visualEffectEnabled;
                if (_repeller != null) {
                    _repeller.enabled = visualEffectEnabled;
                }
            }
        }
    }
}
