using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.UI.PhotoMode {
    [RequireComponent(typeof(Volume))]
    public class VCPhotoModeSaturationController : ViewComponent<PhotoModeUI> {
        [SerializeField] Volume volume;
        
        ColorAdjustments _colorAdjustments;

        protected override void OnAttach() {
            Target.ListenTo(PhotoModeUI.Events.SaturationChanged, OnSaturationChanged, this);
        }

        void OnSaturationChanged(float value) {
            if (_colorAdjustments == null) {
                volume.TryGetVolumeComponent(out _colorAdjustments);
            }
            
            _colorAdjustments.saturation.value = value;
        }

        void Reset() {
            volume = GetComponent<Volume>();
        }

        protected override void OnDiscard() {
            OnSaturationChanged(0f);
        }
    }
}