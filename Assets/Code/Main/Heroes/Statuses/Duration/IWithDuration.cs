using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public interface IWithDuration : IModel {
        IModel TimeModel { get; }
        bool CanEvaluateTime => true;
    }
}