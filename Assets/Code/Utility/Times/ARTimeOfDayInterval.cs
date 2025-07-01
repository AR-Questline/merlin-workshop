using System;
using UnityEngine;

namespace Awaken.Utility.Times {
    [Serializable]
    public struct ARTimeOfDayInterval {
        [SerializeField] ARTimeOfDay from;
        [SerializeField] ARTimeOfDay to;
        
        public readonly ARTimeOfDay From => from;
        public readonly ARTimeOfDay To => to;
        
        public ARTimeOfDayInterval(ARTimeOfDay from, ARTimeOfDay to) {
            this.from = from;
            this.to = to;
        }
    }

    public struct ARTimeOfDayIntervalRuntime {
        TimeSpan _from;
        TimeSpan _to;
        
        public readonly TimeSpan From => _from;
        public readonly TimeSpan To => _to;
        
        public ARTimeOfDayIntervalRuntime(in ARTimeOfDayInterval interval) {
            _from = interval.From.GetTime();
            _to = interval.To.GetTime();
        }

        public readonly bool Contains(TimeSpan time) {
            if (_from <= _to) {
                return time >= _from && time <= _to;
            } else {
                // it means that the time range is between two days
                return time >= _from || time <= _to;
            }
        }
    }
}