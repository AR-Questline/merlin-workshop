using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public readonly struct InteractionInterval {
        readonly int _startHours;
        readonly int _startMinutes;
        readonly int _startDeviation;
        readonly InteractionSource _original;
        readonly InteractionSource _rainSource;

        public InteractionInterval(int startHours, int startMinutes, int startDeviation, InteractionSource original, InteractionSource rainSource) {
            this._startHours = startHours;
            this._startMinutes = startMinutes;
            this._startDeviation = startDeviation;
            this._original = original;
            this._rainSource = rainSource;
        }

        public static readonly InteractionInterval Fallback = new(0, 0, 0, new(new InteractionFallbackFinder(), FallbackInteractionData.Default), null);

        public DateTime ThisDayStartTime(DateTime currentTime, bool withDeviation = false) {
            var date = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, _startHours, _startMinutes, 0);
            if (withDeviation && _startDeviation > 0) {
                date = date.AddMinutes(RandomUtil.UniformInt(0, _startDeviation));
            }
            return date;
        }
            
        public IInteractionSource GetCurrentSource() {
            if (_rainSource != null && ShouldPerformRainCustomAction) {
                return _rainSource;
            }
            return _original;
        }
            
        public static int CompareDate(InteractionInterval a, InteractionInterval b) {
            return CompareDate(a._startHours, a._startMinutes, b._startHours, b._startMinutes);
        }
            
        public static int CompareDate(int lhsHour, int lhsMinutes, int rhsHour, int rhsMinutes) {
            if (lhsHour < rhsHour) {
                return -1;
            } else if (lhsHour > rhsHour) {
                return 1;
            } else if (lhsMinutes < rhsMinutes) {
                return -1;
            } else if (lhsMinutes > rhsMinutes) {
                return 1;
            } else {
                return 0;
            }
        }
        
        static bool ShouldPerformRainCustomAction => World.Only<WeatherController>().HeavyRain;
    }
}