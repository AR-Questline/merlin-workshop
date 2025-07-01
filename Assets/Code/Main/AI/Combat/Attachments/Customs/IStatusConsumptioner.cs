using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    public interface IStatusConsumptioner : IModel {
        public CharacterStatuses StatusesOwner { get; }
        public bool CanConsume(StatusTemplate status);
        public void OnStatusDiscarded();
    }
}