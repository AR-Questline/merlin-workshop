using System;
using System.Runtime.CompilerServices;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Threads;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Timing {
    /// <summary>
    /// PlayRealTime and WeatherTime can be paused using a <see cref="TimeBlocker"/> or modified by adding an <see cref="ITimeModifier"/>
    /// </summary>
    public partial class GameRealTime : Model {
        public override ushort TypeForSerialization => SavedModels.GameRealTime;

        public override Domain DefaultDomain => Domain.Gameplay;
        // === Fields & Properties
        const string PauseSourceID = "gamerealtime.pause";

        float _weatherSecondsPerRealSecond;
        TimeDependentsCache _timeDependentsCache;
        // Real time the player has spent in the game
        [Saved] public ARTimeSpan PlayRealTime { get; private set; }
        // Time used for NPC routines and weather, default is set from Game Constants
        [Saved] public ARDateTime WeatherTime { get; private set; }

        public float WeatherSecondsPerRealSecond => _weatherSecondsPerRealSecond;
        [UnityEngine.Scripting.Preserve] public double PlayRealTimeInSeconds => PlayRealTime.TotalSeconds;

        public event Action<bool> NightChanged;
        public event Action TimeScaleChanged;

        // === Initialization
        protected override void OnInitialize() {
            GameConstants gc = Services.Get<GameConstants>();
            WeatherTime = new DateTime(gc.gameStartYear, gc.gameStartMonth, gc.gameStartDay, gc.gameStartHour, gc.gameStartMinute, 0);
            AddElement<WeatherController>();
            Init();
        }

        protected override void OnRestore() {
            Init();
        }

        void Init() {
            ARTimeUtils.GetOrCreateTimeDependent(this).WithTimeScaleChanged(TriggerTimeScaleChanged);
            _timeDependentsCache = AddElement<TimeDependentsCache>();
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChange, this);
            AddElement(new GameTimeEvents());
            OnUIStateChange(UIStateStack.Instance.State);
            float dayDurationInMinutes = World.Services.Get<GameConstants>().dayDurationInMinutes;
            _weatherSecondsPerRealSecond = 24f * 60f / dayDurationInMinutes;
        }

        // === Processing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunTimeDependentModelsUpdate(TimeDependentsCache timeDependentsCache) {
            ThreadSafeUtils.AssertMainThread();
            foreach (var t in timeDependentsCache) {
                try {
                    if (t.HasUpdate && t.CanProcess) {
                        t.ProcessUpdate();
                    }
                    t.ProcessAlwaysUpdate();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunTimeDependentModelsLateUpdate(TimeDependentsCache timeDependentsCache) {
            ThreadSafeUtils.AssertMainThread();
            foreach (var t in timeDependentsCache) {
                try {
                    if (t.HasLateUpdate && t.CanProcess) {
                        t.ProcessLateUpdate();
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunTimeDependentModelsFixedUpdate(TimeDependentsCache timeDependentsCache) {
            ThreadSafeUtils.AssertMainThread();
            foreach (var t in timeDependentsCache) {
                try {
                    if (t.HasFixedUpdate && t.CanProcess) {
                        t.ProcessFixedUpdate();
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void ProcessFixedUpdate() {
            RunTimeDependentModelsFixedUpdate(_timeDependentsCache);
        }
        public void ProcessUpdate() {
            Perception.NextFrame();

            PlayRealTime += (ARTimeSpan)TimeSpan.FromSeconds(Time.deltaTime);
            if (!UIStateStack.Instance.State.PauseWeatherTime) {
                WeatherIncrementSeconds(Time.deltaTime * _weatherSecondsPerRealSecond);
            }
            RunTimeDependentModelsUpdate(_timeDependentsCache);
        }
        public void ProcessLateUpdate() {
            RunTimeDependentModelsLateUpdate(_timeDependentsCache);
        }

        public void SetWeatherTime(int hour, int minute, int? seconds = null, int? milliseconds = null) {
            hour = Mathf.Clamp(hour, 0, 23);
            minute = Mathf.Clamp(minute, 0, 59);
            
            DateTime currentTime = (DateTime)WeatherTime;
            DateTime desiredTime = new(currentTime.Year, currentTime.Month, currentTime.Day, hour, minute,
                seconds ?? currentTime.Second, milliseconds ?? currentTime.Millisecond);
                
            if (desiredTime < currentTime) {
                desiredTime = desiredTime.AddDays(1);
            }
            float secondsDiff = (float) (desiredTime - currentTime).TotalSeconds;
            float minutesDiff = secondsDiff / 60;
            
            var timeSkippedData = new TimeSkipData {
                timeSkippedInMinutes = minutesDiff,
                safelySkipping = false
            };
            this.Trigger(Events.BeforeTimeSkipped, timeSkippedData);
            WeatherIncrementSeconds(secondsDiff);
            foreach (var spawner in World.All<BaseLocationSpawner>()) {
                spawner.AfterTimeSkipped(timeSkippedData);
            }
        }

        public void SkipWeatherTimeBy(int hours, int minutes, bool safelySkipped = false) {
            hours = Mathf.Max(0, hours);
            minutes = Mathf.Clamp(minutes, 0, 59);

            DateTime currentTime = (DateTime)WeatherTime;
            DateTime futureTime = currentTime.AddHours(hours).AddMinutes(minutes);

            float secondsDiff = (float) (futureTime - currentTime).TotalSeconds;
            float minutesDiff = secondsDiff / 60;
            
            var timeSkippedData = new TimeSkipData {
                timeSkippedInMinutes = minutesDiff,
                safelySkipping = safelySkipped
            };
            this.Trigger(Events.BeforeTimeSkipped, timeSkippedData);
            WeatherIncrementSeconds(secondsDiff);
            foreach (var spawner in World.All<BaseLocationSpawner>()) {
                spawner.AfterTimeSkipped(timeSkippedData);
            }
        }

        public void WeatherIncrementSeconds(float seconds) {
            bool wasNight = WeatherTime.IsNight;
            WeatherTime = WeatherTime.IncrementSeconds(seconds);
            this.Trigger(Events.GameTimeChanged, WeatherTime);
            if (!wasNight && WeatherTime.IsNight) {
                this.Trigger(Events.NightBegan, WeatherTime);
                NightChanged?.Invoke(true);
            } else if (wasNight && !WeatherTime.IsNight) {
                this.Trigger(Events.DayBegan, WeatherTime);
                NightChanged?.Invoke(false);
            }
        }
        
        public void WeatherIncrementDayFloat(float time) {
            WeatherIncrementSeconds(time * 24 * 60 * 60);
        }

        public void SetWeatherDayDuration(float minutes) {
            _weatherSecondsPerRealSecond = 24f*60f/minutes;
        }

        public bool WillSkipTimeBeInterrupted(float skipTimeInHours, bool safelySkipping, out float skipTimeInSecondsTillInterrupt) {
            DateTime currentTime = (DateTime)WeatherTime;
            int hours = Mathf.FloorToInt(skipTimeInHours);
            int minutes = Mathf.FloorToInt((skipTimeInHours - hours) * 60f);
            if (WillSkipTimeBeInterrupted(currentTime, hours, minutes, safelySkipping, out DateTime interruptTime)) {
                skipTimeInSecondsTillInterrupt = (float) (interruptTime - currentTime).TotalSeconds;
                return true;
            }
            skipTimeInSecondsTillInterrupt = -1;
            return false;
        }

        public bool WillSkipTimeBeInterrupted(DateTime currentTime, int hours, int minutes, bool safelySkipping, out DateTime interruptTime)  {
            interruptTime = currentTime;
            while (hours > 0 || minutes > 0) {
                if (hours > 0) {
                    interruptTime = interruptTime.AddHours(1);
                    hours--;
                } else {
                    interruptTime = interruptTime.AddMinutes(minutes);
                    minutes = 0;
                }
                var timeSkipData = new TimeSkipData {
                    timeSkippedInMinutes = (int) (interruptTime - currentTime).TotalMinutes,
                    safelySkipping = safelySkipping
                };
                var timeSkipPrevented = false;
                foreach (var spawner in World.All<BaseLocationSpawner>()) {
                    spawner.InterruptTimeSkipCheck(timeSkipData, ref timeSkipPrevented);
                }
                if (timeSkipPrevented) {
                    return true;
                }
            }
            return false;
        }
        
        // === Modifiers
        [UnityEngine.Scripting.Preserve]
        public void AddTimeModifier(ITimeModifier modifier) {
            ARTimeUtils.AddTimeModifier(this, modifier);
            TriggerTimeScaleChanged();
        }
        
        [UnityEngine.Scripting.Preserve]
        public void RemoveTimeModifiersFor(string sourceID) {
            ARTimeUtils.RemoveTimeModifiersFor(this, sourceID);
        }

        void TriggerTimeScaleChanged() {
            TimeScaleChanged?.Invoke();
        }
        
        void TriggerTimeScaleChanged(float from, float to) {
            TriggerTimeScaleChanged();
        }

        // === Pause
        void OnUIStateChange(UIState uiState) {
            if (uiState.PauseTime) {
                PauseTime();
            } else {
                UnpauseTime();
            }
        }

        void PauseTime() {
            World.Only<GlobalTime>().AddTimeModifier(new OverrideTimeModifier(PauseSourceID, 0));
        }
        void UnpauseTime() {
            World.Only<GlobalTime>().RemoveTimeModifiersFor(PauseSourceID);
        }

        public void PauseARTime() {
            AddTimeModifier(new OverrideTimeModifier(PauseSourceID, 0));
        }
        public void UnpauseARTime() {
            RemoveTimeModifiersFor(PauseSourceID);
        }

        // === String
        public override string ToString() {
            return $"{WeatherTime.Hour}:{WeatherTime.Minutes}";
        }

        // === Events
        public new static class Events {
            public static readonly Event<GameRealTime, ARDateTime> GameTimeChanged = new(nameof(GameTimeChanged));
            public static readonly Event<GameRealTime, ARDateTime> DayBegan = new(nameof(DayBegan));
            public static readonly Event<GameRealTime, ARDateTime> NightBegan = new(nameof(NightBegan));
            public static readonly Event<GameRealTime, TimeSkipData> BeforeTimeSkipped = new(nameof(BeforeTimeSkipped));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            NightChanged = null;
            TimeScaleChanged = null;
            base.OnDiscard(fromDomainDrop);
        }

        public struct TimeSkipData {
            public float timeSkippedInMinutes;
            public bool safelySkipping;
        }
    }
}