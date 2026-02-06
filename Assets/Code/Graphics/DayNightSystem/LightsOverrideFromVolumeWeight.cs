using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.VFX {
    public class LightsOverrideFromVolumeWeight : LightsOverride {
        [SerializeField, Required] Volume volume;
        protected override bool RunUpdateInEditMode => true;

        public override void OnLateUpdate(float deltaTime) {
#if UNITY_EDITOR
            if (Application.isPlaying == false && volume == null) {
                return;
            }
#endif
            if (volume.weight <= float.Epsilon) {
                StopOverride();
                return;
            }

            var blendingFactor = volume.weight;
            ApplyLightOverrides(deltaTime, blendingFactor);
        }

        protected override void Setup() {
            if (Application.isPlaying && !volume) {
                Log.Important?.Error("No volume assigned to LightsOverrideFromVolumeWeight", this);
                Destroy(this);
                return;
            }
            base.Setup();
        }
    }
}