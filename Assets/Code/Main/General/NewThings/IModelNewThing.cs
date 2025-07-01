using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.General.NewThings {
    public interface IModelNewThing : IModel {
        string NewThingId { get; }
        bool DiscardAfterMarkedAsSeen { get; }

        public static class Events {
            public static readonly Event<IModelNewThing, IModelNewThing> NewThingRefreshed = new(nameof(NewThingRefreshed));
        } 
    }
}