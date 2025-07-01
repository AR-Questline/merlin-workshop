using System.Diagnostics;

namespace Awaken.TG.MVC.Events {
    public interface IEvent {
        string Name { get; }
        bool CanBeQueued { get; }
    }

    public interface IEvent<in TSource, in TPayload> : IEvent { }

    /// <summary>
    /// Parent model of Event.
    /// </summary>
    [DebuggerDisplay("{Name} Event")]
    public class Event : IEvent {
        public string Name { get; }
        public bool CanBeQueued { get; }

        protected Event(string name, bool canBeQueued = false) {
            Name = name;
            CanBeQueued = canBeQueued;
        }
    }
    /// <summary>
    /// Represents a type of event.
    /// A static event object is created for each type of event that can be triggered by a model.
    /// This allows for a type-safe event system.
    /// </summary>
    /// <typeparam name="TSource">type of object on which this event can be triggered</typeparam>
    /// <typeparam name="TPayload">the payload delivered by this event, which can be of any type</typeparam>
    [DebuggerDisplay("{Name} Event<{typeof(TSource).Name,nq}, {typeof(TPayload).Name,nq}>")]
    public class Event<TSource, TPayload> : Event, IEvent<TSource, TPayload> {
        public Event(string name, bool canBeQueued = false) : base(name, canBeQueued) { }
    }
}