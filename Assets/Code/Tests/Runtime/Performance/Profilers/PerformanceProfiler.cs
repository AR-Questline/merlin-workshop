using System.Collections.Generic;

namespace Awaken.Tests.Performance.Profilers {
    public abstract class PerformanceProfiler<TCapture> : IPerformanceProfiler where TCapture : struct {
        readonly List<TCapture> _captures = new();

        public TCapture GetFrame(int index) => _captures[index];
        
        public void Start() {
            OnStart();
        }

        public void Update() {
            if (TryCapture(out var capture)) {
                _captures.Add(capture);
            } else {
                _captures.Add(_captures.Count == 0 ? default : _captures[^1]);
            }
        }

        public void End() {
            _captures.Clear();
            OnEnd();
        }

        protected abstract bool TryCapture(out TCapture capture);
        protected virtual void OnStart() { }
        protected virtual void OnEnd() { }
    }
}