using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public class TransformScaleEventEmitter : VoiceOversEventEmitter {
        const float ScaleUpdateSpeed = 0.5f;
        
        [SerializeField] Transform transformToScale;
        Vector3 _originalScale;
        DelayedVector3 _scaleDelta;
        bool _active;
        
        protected override void Awake() {
            base.Awake();
            _originalScale = transformToScale.localScale;
            _scaleDelta.SetInstant(Vector3.zero);
        }
        
        protected override void SpeakingUpdate(double timePlaying) {
            _scaleDelta.Set(GetExternalAnalysis().UniformVector3());
            _active = true;
        }

        protected override void OnSpeakingEnded() {
            _scaleDelta.Set(Vector3.zero);
        }

        void Update() {
            if (!_active) {
                return;
            }
            
            if (_scaleDelta.Value.EqualsApproximately(Vector3.zero, 0.01f)) {
                transformToScale.localScale = _originalScale;
                _active = false;
            }
            
            _scaleDelta.Update(Time.deltaTime, ScaleUpdateSpeed);
            transformToScale.localScale = _originalScale + _scaleDelta.Value;
        }
    }
}