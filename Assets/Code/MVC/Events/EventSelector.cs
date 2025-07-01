using System;

namespace Awaken.TG.MVC.Events {
    /// <summary>
    /// Triggered events are matched to listeners using these selectors.
    /// </summary>
    public struct EventSelector : IEquatable<EventSelector> {

        // === Constants

        public const string AnySource = "*";

        // === Properties

        public string TargetPattern { get; private set; }
        public IEvent Event { get; private set; }

        // === Constructors

        public EventSelector(string targetPattern, IEvent @event) {
            TargetPattern = targetPattern;
            Event = @event;
        }

        // === Behavior

        public bool Equals(EventSelector other) {
            return ReferenceEquals(Event, other.Event) && string.Equals(TargetPattern, other.TargetPattern);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is EventSelector selector && Equals(selector);
        }

        public override int GetHashCode() {
            unchecked {
                return (TargetPattern.GetHashCode() * 397) ^ Event.GetHashCode();
            }
        }

        public override string ToString() {
            return $"{TargetPattern}|{Event.Name}";
        }
    }
}