using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.Utility.Times {
    [Serializable]
    public partial struct ARTimeSpan : IEquatable<ARTimeSpan> {
        public ushort TypeForSerialization => SavedTypes.ARTimeSpan;

        [field: SerializeField] [Saved(0L)] public long Ticks { get; set; }

        public ARTimeSpan(long ticks) {
            Ticks = ticks;
        }

        public int Days {
            get => ((TimeSpan)this).Days;
            set => Ticks += TimeSpan.TicksPerDay*(value - Days);
        }

        public int Hours {
            get => ((TimeSpan)this).Hours;
            set => Ticks += TimeSpan.TicksPerHour*(value - Hours);
        }

        public int Minutes {
            get => ((TimeSpan)this).Minutes;
            set => Ticks += TimeSpan.TicksPerMinute*(value - Minutes);
        }
        
        public int Seconds {
            get => ((TimeSpan)this).Seconds;
            set => Ticks += TimeSpan.TicksPerSecond*(value - Seconds);
        }

        public float TotalHours => (float)((TimeSpan)this).TotalHours;
        public float TotalMinutes => (float)((TimeSpan)this).TotalMinutes;
        public float TotalSeconds => (float)((TimeSpan)this).TotalSeconds;
        [UnityEngine.Scripting.Preserve] public float TotalMilliseconds => (float)((TimeSpan)this).TotalMilliseconds;

        public static implicit operator TimeSpan(ARTimeSpan arTimeSpan) => new(arTimeSpan.Ticks);
        public static implicit operator ARTimeSpan(TimeSpan timeSpan) => new() { Ticks = timeSpan.Ticks };
        
        public static ARTimeSpan operator +(ARTimeSpan a, ARTimeSpan b) => new(a.Ticks + b.Ticks);
        public static ARTimeSpan operator -(ARTimeSpan a, ARTimeSpan b) => new(a.Ticks - b.Ticks);

        public bool Equals(ARTimeSpan other) {
            return Ticks == other.Ticks;
        }
        public override bool Equals(object obj) {
            return obj is ARTimeSpan other && Equals(other);
        }
        public override int GetHashCode() {
            return Ticks.GetHashCode();
        }
        public static bool operator ==(ARTimeSpan left, ARTimeSpan right) {
            return left.Equals(right);
        }
        public static bool operator !=(ARTimeSpan left, ARTimeSpan right) {
            return !left.Equals(right);
        }
    }
}
