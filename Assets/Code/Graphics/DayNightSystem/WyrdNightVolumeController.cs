using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class WyrdNightVolumeController : WyrdNightControllerBase {
        Volume _volume;

        protected override void OnAwake() {
            _volume = GetComponent<Volume>();
            
            if (_volume == null) {
                Debug.LogError("No Volume component found on " + gameObject.HierarchyPath());
                enabled = false;
                return;
            }
            ApplyEffect(EnabledValue);
        }

        protected override void ApplyEffect(float value) {
            _volume.weight = value;
        }
    }
}