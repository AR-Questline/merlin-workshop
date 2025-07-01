using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public interface IDurationProxy : IElement {
        IDuration Duration { get; }
    }
    
    public abstract partial class DurationProxy<T> : Element<T>, IWithDuration, IDuration, IDurationProxy where T : IModel {
        public abstract IModel TimeModel { get; }
        public virtual bool CanEvaluateTime => true;

        IDuration _cachedDuration;
        public IDuration Duration => TryGetCachedElementWithChecks(ref _cachedDuration);
        public bool Elapsed => Duration.Elapsed;
        public string DisplayText => Duration.DisplayText;

        [JsonConstructor, UnityEngine.Scripting.Preserve] protected DurationProxy() { }
        protected DurationProxy(IDuration duration) {
            AddElement(duration);
        }

        protected override void OnFullyInitialized() {
            if (Duration == null) {
                DurationDiscarded(null);
            } else {
                Duration.ListenTo(Events.AfterDiscarded, DurationDiscarded, this);
            }
        }

        public virtual void Prolong(IDuration duration) {
            Duration.Prolong(duration);
        }

        public virtual void Renew(IDuration duration) {
            Duration.Renew(duration);
        }

        public void ResetDuration() {
            Duration.ResetDuration();
        }

        public void ReduceTimeSeconds(float seconds) {
            if (Duration is TimeDuration td) {
                td.ReduceTimeSeconds(seconds);
            }
        }

        public void ReduceTime(float percentage) {
            Duration.ReduceTime(percentage);
        }
        
        void DurationDiscarded(Model _) {
            if (HasBeenDiscarded) {
                return;
            }

            OnDurationElapsed();
        }

        protected virtual void OnDurationElapsed() {
            Discard();
        }
    }
}