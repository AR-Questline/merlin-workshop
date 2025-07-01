using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public interface IDuration : IElement {
        bool Elapsed { get; }
        string DisplayText { get; }

        void Prolong(IDuration duration);
        void Renew(IDuration duration);
        void ResetDuration();
        void ReduceTime(float percentage);

        public static class Events {
            public static readonly HookableEvent<IDuration, bool> Elapsed = new(nameof(Elapsed));
        }
    }
}