namespace Awaken.TG.MVC.Events {
    /// <summary>
    /// Data of queued Event (triggered at the end of the frame).
    /// </summary>
    public struct TriggerData {
        public IEventSource Source { get; }
        public IEvent Event { get; }
        public object Payload { get; }

        public TriggerData(IEventSource source, IEvent evt, object payload) {
            Source = source;
            Event = evt;
            Payload = payload;
        }
    }
}