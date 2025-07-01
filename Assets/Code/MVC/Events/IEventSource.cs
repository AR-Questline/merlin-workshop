namespace Awaken.TG.MVC.Events
{
    /// <summary>
    /// Interface for event sources - all that's required is having a unique ID.
    /// Currently, models and views both work as event sources.
    /// </summary>
    public interface IEventSource
    {
        string ID { get; }
    }
}
