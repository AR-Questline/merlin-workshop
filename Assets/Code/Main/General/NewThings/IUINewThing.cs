using System;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.General.NewThings {
    /// <summary>
    /// Used on UI that is responsible for displaying the 'Thing' or group of 'Things'
    /// Might be Model, View or Component
    /// </summary>
    public interface IUINewThing {
        bool IsInitialized { get; }
        bool IsNew { get; }
        event Action onNewThingRefresh;
    }
    
    /// <summary>
    /// Placed on objects that display a single 'Thing' 
    /// </summary>
    public interface INewThingCarrier : IUINewThing {
        bool IUINewThing.IsInitialized => NewThingModel != null;
        bool IUINewThing.IsNew => !World.Services.Get<NewThingsTracker>().WasSeen(NewThingId);
        IModelNewThing NewThingModel { get; }
        string NewThingId => NewThingModel?.NewThingId;
        void MarkSeen() => World.Services.Get<NewThingsTracker>().MarkSeen(this);
    }

    /// <summary>
    /// Used on components that don't directly correspond to a single item, but rather represent a group of some sort.
    /// For example Bag
    /// </summary>
    public interface INewThingContainer : IUINewThing {
        bool IUINewThing.IsInitialized => true;
        bool IUINewThing.IsNew => World.Services.Get<NewThingsTracker>().ProxyHasNewThings(this);
        bool NewThingBelongsToMe(IModel model);
        void RefreshNewThingsContainer();
    }
}