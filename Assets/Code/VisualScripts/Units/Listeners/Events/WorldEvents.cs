using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.Utility.Times;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/World")]
    public abstract class EvtWorld<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtDayBegan : EvtWorld<GameRealTime, ARDateTime> {
        protected override Event<GameRealTime, ARDateTime> Event => GameRealTime.Events.DayBegan;
        protected override GameRealTime Source(IListenerContext context) => World.Only<GameRealTime>();
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtNightBegan : EvtWorld<GameRealTime, ARDateTime> {
        protected override Event<GameRealTime, ARDateTime> Event => GameRealTime.Events.NightBegan;
        protected override GameRealTime Source(IListenerContext context) => World.Only<GameRealTime>();
    }
}