using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Custom.NotToFrequents {
    /// <summary>
    /// It can ensures that your code is not called too frequently.
    /// You need to call Try method with priority to check if you can call your code.
    /// Try will return true if all calls of Try with this priority and above returned true its interval ago.
    /// </summary>
    public class NotTooFrequent {
        readonly float[] _intervals;
        readonly float[] _nextTime;
        
        public NotTooFrequent(float[] intervals) {
            _intervals = intervals;
            _nextTime = new float[intervals.Length];
        }

        public bool Try(int priority) {
            if (priority < 0 || priority >= _intervals.Length) {
                return false;
            }
            var time = Time.time;
            if (time < _nextTime[priority]) {
                return false;
            }
            for (int i = priority; i < _nextTime.Length; i++) {
                _nextTime[i] = time + _intervals[i];
            }
            return true;
        }
    }
}