using System;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public class TimeTrailRenderer : ITimeComponent {
        const float Delay = 0.1f;
        const float Epsilon = 0.0001f;

        readonly TrailRenderer _trail;
        readonly float _baseTime;
        Sequence _sequence;
        
        public Component Component => _trail;
        
        public TimeTrailRenderer(TrailRenderer trailRenderer) {
            _trail = trailRenderer;
            _baseTime = trailRenderer.time;
        }

        public void OnTimeScaleChange(float from, float to) {
            _sequence.Kill();

            if (to < Epsilon) {
                to = Epsilon;
            }

            _sequence = DOTween.Sequence()
                .Append(DOTween.To(() => _trail.time, t => _trail.time = t, _baseTime / to, Delay));
        }

        public void OnFixedUpdate(float fixedDeltaTime) {
            if (_trail == null) {
                _sequence.Kill();
            }
        }
    }
}