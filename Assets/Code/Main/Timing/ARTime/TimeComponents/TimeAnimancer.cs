using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public class TimeAnimancer : ITimeComponent {
        readonly AnimancerComponent _animancer;

        public Component Component => _animancer;
        
        public void OnTimeScaleChange(float from, float to) {
            _animancer.Playable.Speed = to;
        }

        public void OnFixedUpdate(float fixedDeltaTime) { }

        
        public TimeAnimancer(AnimancerComponent animancer) {
            _animancer = animancer;
        }
    }
}