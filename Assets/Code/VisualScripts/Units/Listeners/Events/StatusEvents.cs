using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/Statuses")]
    public abstract class EvtStatus<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtStatusAdded : EvtStatus<CharacterStatuses, Status> {
        protected override Event<CharacterStatuses, Status> Event => CharacterStatuses.Events.AddedStatus;
        protected override CharacterStatuses Source(IListenerContext context) => context.Character.Statuses;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtStatusVanished : EvtStatus<CharacterStatuses, Status> {
        protected override Event<CharacterStatuses, Status> Event => CharacterStatuses.Events.VanishedStatus;
        protected override CharacterStatuses Source(IListenerContext context) => context.Character.Statuses;
    }
}