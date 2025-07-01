
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.DayNightSystem {
    [RequireComponent(typeof(Volume))]
    public class DayNightSystemVolumeController : DayNightSystemComponentController {
        
        [SerializeField]
        AnimationCurve animationCurve;

        Volume _volume;
        
        protected override void Init() {
            _volume = gameObject.GetComponent<Volume>();
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (_volume != null) {
                _volume.weight = animationCurve.Evaluate(TimeOfDay);
            }
        }
    }
}
