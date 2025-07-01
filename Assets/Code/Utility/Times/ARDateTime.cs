using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;

namespace Awaken.Utility.Times {
    /// <summary>
    /// Wrapper for DateTime with useful properties and operators for easy conversions
    /// </summary>
    [Serializable]
    public struct ARDateTime : IEquatable<ARDateTime>, IComparable<ARDateTime> {
        public ushort TypeForSerialization => SavedTypes.ARDateTime;

        public const float NightStart = 0.92f, NightEnd = 0.23f;
        public static TimeSpan NightStartTime => TimeSpan.FromHours(NightStart * 24);
        public static TimeSpan NightEndTime => TimeSpan.FromHours(NightEnd * 24);
        
        [Saved] DateTime _dateTime;

        public DateTime Date => _dateTime;
        public int Day => _dateTime.DayOfYear;
        [UnityEngine.Scripting.Preserve] public int DayOfTheWeek => GameTimeUtil.DayOfTheWeek(Day);
        public int DayOfTheMonth => _dateTime.Day;
        public int Week => GameTimeUtil.Week(Day);
        public int Month => _dateTime.Month;
        public int Year => _dateTime.Year;
        public float DayTime => (float) _dateTime.TimeOfDay.TotalMinutes / 1440f;
        public int Hour => _dateTime.Hour;
        public int Minutes => _dateTime.Minute;
        public double TotalSeconds => AsTimeSpan().TotalSeconds;
        public long Ticks => _dateTime.Ticks;

        public bool IsDay => !IsNight;
        public bool IsNight => DayTime is > NightStart or < NightEnd;

        public ARDateTime(DateTime date) {
            _dateTime = date;
        }

        public static float HoursTillNightEnd(DateTime date) {
            return HoursTill(date, NightEndTime);
        }
        
        public static float HoursTillNightStart(DateTime date) {
            return HoursTill(date, NightStartTime);
        }

        public static float HoursTill(DateTime from, TimeSpan to) {
            if (to.Hours > from.Hour) {
                return (float) (to.TotalHours - from.TimeOfDay.TotalHours);
            } else {
                return 24f - ((float) (from.TimeOfDay.TotalHours - to.TotalHours));
            }
        }

        public ARDateTime IncrementSeconds(float seconds) {
            DateTime newDateTime;
            if (seconds > 0) {
                newDateTime = _dateTime + TimeSpan.FromSeconds(seconds);
            } else {
                newDateTime = _dateTime - TimeSpan.FromSeconds(-seconds);
            }
            return newDateTime;
        }
        
        public static ARDateTime operator +(ARDateTime dt, TimeSpan ts) {
            return dt._dateTime + ts;
        }

        public static ARDateTime operator +(ARDateTime dt, ARTimeSpan ts) {
            return dt._dateTime + ts;
        }
        
        public static ARDateTime operator -(ARDateTime dt, TimeSpan ts) {
            return dt._dateTime - ts;
        }

        public static ARDateTime operator -(ARDateTime dt, ARTimeSpan ts) {
            return dt._dateTime - ts;
        }

        public static TimeSpan operator -(ARDateTime a, ARDateTime b) {
            return a._dateTime - b._dateTime;
        }

        // === Equality
        public bool Equals(ARDateTime other) {
            return _dateTime.Equals(other._dateTime);
        }
        public override bool Equals(object obj) {
            return obj is ARDateTime other && Equals(other);
        }
        public override int GetHashCode() {
            return _dateTime.GetHashCode();
        }
        public static bool operator ==(ARDateTime left, ARDateTime right) {
            return left.Equals(right);
        }
        public static bool operator !=(ARDateTime left, ARDateTime right) {
            return !left.Equals(right);
        }

        public int CompareTo(ARDateTime other) {
            return _dateTime.CompareTo(other._dateTime);
        }
        public static bool operator <(ARDateTime left, ARDateTime right) {
            return left.CompareTo(right) < 0;
        }
        public static bool operator >(ARDateTime left, ARDateTime right) {
            return left.CompareTo(right) > 0;
        }
        public static bool operator <=(ARDateTime left, ARDateTime right) {
            return left.CompareTo(right) <= 0;
        }
        public static bool operator >=(ARDateTime left, ARDateTime right) {
            return left.CompareTo(right) >= 0;
        }

        // === Conversion
        public static explicit operator DateTime(ARDateTime arDateTime) => arDateTime._dateTime;
        public static implicit operator ARDateTime(DateTime dateTime) => new(dateTime);
        TimeSpan AsTimeSpan() => new(_dateTime.Ticks);
    }
}