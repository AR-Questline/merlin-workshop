using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Saturation {
    [ExecuteAlways]
    public class SaturationController : MonoBehaviour {
        [SerializeField, Range(-1, 1), OnValueChanged(nameof(OnSaturationChanged)), LabelText("Saturation")] float saturationAdjustment;
        
        public float SaturationAdjustment => saturationAdjustment;

        void OnEnable() {
            SaturationStack.Instance.Add(this);
        }

        void OnDisable() {
            SaturationStack.Instance.Remove(this);
        }

        void OnSaturationChanged() {
            SaturationStack.Instance.OnSaturationChanged(this);
        }
    }
}