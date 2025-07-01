using System;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Availability {
    [Serializable]
    public class DayNightAvailability : AvailabilityBase, IEquatable<DayNightAvailability> {
        [SerializeField] bool atDay = true;
        [SerializeField] bool atNight = true;

        protected override bool SceneInitializationNeeded => CanBeUnavailable;
        bool CanBeUnavailable => !atNight | !atDay;

        protected override void OnSceneInitialized() {
            World.Only<GameRealTime>().NightChanged += NightChanged;
        }
        
        protected override void DisposeListeners() {
            var gameRealTime = World.Any<GameRealTime>();
            if (gameRealTime != null) {
                gameRealTime.NightChanged -= NightChanged;
            }
            base.DisposeListeners();
        }

        protected override bool CalculateAvailability() {
            if (CanBeUnavailable) {
                var gameRealTime = World.Any<GameRealTime>();
                if (gameRealTime == null) {
                    return false;
                }
                bool isNight = gameRealTime.WeatherTime.IsNight;
                bool isDay = gameRealTime.WeatherTime.IsDay;
                if ((!atNight & isNight) | (!atDay & isDay)) {
                    return false;
                }
            }

            return true;
        }

        void NightChanged(bool _) {
            CheckChanged();
        }

        public bool Equals(DayNightAvailability other) {
            if (other is null) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return (atDay == other.atDay) & (atNight == other.atNight);
        }

        public override bool Equals(object obj) {
            if (obj is null) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((DayNightAvailability)obj);
        }

        public override int GetHashCode() {
            return (atDay ? 1 : 0) + (atNight ? 2 : 0);
        }

        public static bool operator ==(DayNightAvailability left, DayNightAvailability right) {
            return Equals(left, right);
        }

        public static bool operator !=(DayNightAvailability left, DayNightAvailability right) {
            return !Equals(left, right);
        }
    }
}