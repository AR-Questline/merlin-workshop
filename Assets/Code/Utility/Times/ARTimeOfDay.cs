using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Times {
    [Serializable]
    public struct ARTimeOfDay {
        [SerializeField] TimeOfDay time;
        [SerializeField, ShowIf(nameof(SpecificHour)), HideLabel, HorizontalGroup] byte hour;
        [SerializeField, ShowIf(nameof(SpecificHour)), HideLabel, HorizontalGroup] byte minutes;
        bool SpecificHour => time == TimeOfDay.SpecificHour;
        
        public readonly TimeSpan GetTime() {
            return time switch {
                TimeOfDay.SpecificHour => new TimeSpan(hour % 24, minutes % 60, 0),
                TimeOfDay.DayStart => ARDateTime.NightEndTime,
                TimeOfDay.NightStart => ARDateTime.NightStartTime,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public enum TimeOfDay : byte {
            SpecificHour,
            NightStart,
            DayStart
        }
    }
}