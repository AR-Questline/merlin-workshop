using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    
    [UnitCategory("AR/General/Events/Locations")]
    public abstract class EvtLocation<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }

    [UnitCategory("AR/General/Events/Locations/Interactions")]
    public abstract class EvtInteraction<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtInteract : EvtLocation<Location, LocationInteractionData> {
        protected override Event<Location, LocationInteractionData> Event => Location.Events.Interacted;
        protected override Location Source(IListenerContext context) => context.Location;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtInteractionItemPicked : EvtInteraction<Location, PickItemAction.ItemPickedData> {
        protected override Event<Location, PickItemAction.ItemPickedData> Event => PickItemAction.Events.ItemPicked;
        protected override Location Source(IListenerContext context) => context.Location;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtItemPickedFromLocation : EvtInteraction<Location, Item> {
        protected override Event<Location, Item> Event => Location.Events.ItemPickedFromLocation;
        protected override Location Source(IListenerContext context) => context.Location;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtVisualLoaded : EvtLocation<Location, GameObject> {
        protected override Event<Location, GameObject> Event => Location.Events.VisualLoaded;
        protected override Location Source(IListenerContext context) => context.Location;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtLocationDiscovered : EvtLocation<LocationDiscovery, Location> {
        protected override Event<LocationDiscovery, Location> Event => LocationDiscovery.Events.LocationDiscovered;
        protected override LocationDiscovery Source(IListenerContext context) => context.Location.TryGetElement<LocationDiscovery>();
    }
}