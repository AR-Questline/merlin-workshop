using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Times {
    public unsafe struct SmoothFpsChecker {
        const int FrameTimeSamples = 5;

        fixed float _frames[FrameTimeSamples];
        readonly int _maxWaitFrameCount;
        float _allowedAmplitude;
        int _nextIndex;
        int _waitedFrames;

        public SmoothFpsChecker(int maxWaitFrameCount, float allowedAmplitude) {
            _maxWaitFrameCount = maxWaitFrameCount;
            _allowedAmplitude = allowedAmplitude;
            _nextIndex = 0;
            _waitedFrames = 0;
        }
        
        public bool FpsAreUnstable() {
            _frames[_nextIndex] = Time.unscaledDeltaTime;
            _nextIndex = (_nextIndex + 1)%FrameTimeSamples;
            ++_waitedFrames;

            if (_waitedFrames < FrameTimeSamples) {
                return true;
            }

            var maxFrameTime = 0f;
            var minFrameTime = float.MaxValue;
            var sum = 0f;
            for (var i = 0; i < FrameTimeSamples; ++i) {
                var frame = _frames[i];
                sum += frame;
                maxFrameTime = math.max(maxFrameTime, frame);
                minFrameTime = math.min(minFrameTime, frame);
            }
            var avg = sum/FrameTimeSamples;
            var maxAmplitude = maxFrameTime - minFrameTime;

            var maxRelativeAmplitude = avg * _allowedAmplitude;
            return _waitedFrames < _maxWaitFrameCount && maxAmplitude > maxRelativeAmplitude;
        }
    }
}
