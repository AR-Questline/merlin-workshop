using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    
    [UnitCategory("AR/General/Events/Models")]
    public abstract class EvtModel<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }

    [UnityEngine.Scripting.Preserve]
    public class EvtDiscarded : EvtModel<IModel, Model> {
        protected override Event<IModel, Model> Event => MVC.Model.Events.AfterDiscarded;
        protected override IModel Source(IListenerContext context) => context.Model;
    }
}