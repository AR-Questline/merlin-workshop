using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Stop Timer Binder")]
    [VFXBinder("AR/Stop Timer Binder")]
    public class VFXStopTimerBinder : VFXBinderBase, IVFXOnStopEffects {
        public string Property { 
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set => property = value;
        }
        
        [VFXPropertyBinding("System.float"), SerializeField]
        ExposedProperty property = "StopTimer";

        [SerializeField] TimerMethod timerMethod = TimerMethod.DeltaTime;
        
        bool _hasStopped;
        float _vfxStoppedTimestamp;

        protected override void OnEnable() {
            base.OnEnable();
            _hasStopped = false;
        }
        
        public override bool IsValid(VisualEffect component)
        {
            return component.HasFloat(property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetFloat(property, GetTimerValue());
        }

        public override string ToString()
        {
            return $"Stop Timer : '{property}' -> {GetTimerValue()}";
        }

        public void VFXStopped() {
            _hasStopped = true;
            _vfxStoppedTimestamp = Time.time;
        }

        float GetTimerValue() {
            if (!_hasStopped) {
                return 0.0f;
            }
            return GetCurrentTimestamp() - _vfxStoppedTimestamp;
        }
        
        float GetCurrentTimestamp() {
            return timerMethod switch {
                TimerMethod.DeltaTime => Time.time,
                TimerMethod.UnscaledDeltaTime => Time.unscaledTime,
                TimerMethod.FixedDeltaTime => Time.fixedTime,
                _ => 0.0f
            };
        }
        
        enum TimerMethod : byte {
            DeltaTime,
            UnscaledDeltaTime,
            FixedDeltaTime,
        }
    }
}