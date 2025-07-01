using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class TimeDuration : Element<IWithDuration>, IDuration {
        public override ushort TypeForSerialization => SavedModels.TimeDuration;

        [Saved] public float OriginalTime { get; private set; }
        [Saved] public float TimeLeft { get; private set; }
        [Saved] bool _unscaledTime;

        public bool Elapsed => TimeLeft <= 0;
        public bool IsInfinite => OriginalTime is <= 0 or float.PositiveInfinity;
        public bool UnscaledTime => _unscaledTime;
        public float TimeLeftNormalized => IsInfinite ? 1f : TimeLeft / OriginalTime;
        public string DisplayText => $"{TimeLeft:F1} {LocTerms.SecondsAbbreviation.Translate()}";
        IModel TimeModel => ParentModel.TimeModel;

        [JsonConstructor, UnityEngine.Scripting.Preserve] protected TimeDuration() { }
        
        public TimeDuration(float time, bool unscaledTime = false) {
            TimeLeft = OriginalTime = time;
            _unscaledTime = unscaledTime;
        }

        protected override void OnInitialize() {
            TimeModel.GetOrCreateTimeDependent().WithAlwaysUpdate(_unscaledTime ? UpdateUnscaled : Update);
        }
        
        void Update(float deltaTime) {
            if (!ParentModel.CanEvaluateTime) {
                return;
            }
            
            TimeLeft -= deltaTime;
            if (Elapsed && !IDuration.Events.Elapsed.RunHooks(this, true).Prevented) {
                Discard();
            }
        }
        
        void UpdateUnscaled(float _) {
            if (!ParentModel.CanEvaluateTime) {
                return;
            }
            
            TimeLeft -= Time.unscaledDeltaTime;
            if (Elapsed && !IDuration.Events.Elapsed.RunHooks(this, true).Prevented) {
                Discard();
            }
        }

        public void ReduceTimeWhenResting(int gameTimeInMinutes) {
            float realTimeChangeInSeconds = gameTimeInMinutes * 60f / World.Only<GameRealTime>().WeatherSecondsPerRealSecond;
            ReduceTimeSeconds(realTimeChangeInSeconds);
        }

        public void Prolong(IDuration duration) {
            if (duration is TimeDuration time) {
                TimeLeft += time.TimeLeft;
            } else {
                Log.Important?.Error($"Cannot prolong {typeof(TimeDuration)} of {Owner} with {duration.GetType()}");
            }
        }

        public void Renew(IDuration duration) {
            if (duration is TimeDuration time) {
                if (time.TimeLeft > TimeLeft) {
                    TimeLeft = OriginalTime = time.TimeLeft;
                }
            } else {
                Log.Important?.Error($"Cannot renew {typeof(TimeDuration)} of {Owner} with {duration.GetType()}");
            }
        }
        
        public void Renew(float time) {
            TimeLeft = OriginalTime = time;
        }

        public void ResetDuration() {
            if (!HasBeenDiscarded) {
                TimeLeft = OriginalTime;
            } else {
                Log.Important?.Error("Refreshed discarded " + nameof(TimeDuration) + "!");
            }
        }

        public void ReduceTimeSeconds(float seconds) {
            TimeLeft -= seconds;
        }

        public void ReduceTime(float percentage) {
            TimeLeft -= TimeLeft * percentage;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            TimeModel.GetTimeDependent()?.WithoutAlwaysUpdate(_unscaledTime ? UpdateUnscaled : Update);
        }

        IModel Owner => ParentModel is IDurationProxy proxy ? proxy.GenericParentModel : ParentModel;
    }
}