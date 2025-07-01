using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public abstract partial class NonEditableDuration<T> : Element<T>, IDuration where T : IModel {
        public abstract bool Elapsed { get; }
        public abstract string DisplayText { get; }
        public void Prolong(IDuration duration) { }
        public void Renew(IDuration duration) { }
        public void ResetDuration() { }
        public void ReduceTime(float percentage) { }
    }
}