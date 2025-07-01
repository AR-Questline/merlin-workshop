namespace Awaken.TG.MVC {
    /// <summary>
    /// This interface is used to retrieve name in consistent way from all models, that might provide a name
    /// </summary>
    public interface INamed {
        string DisplayName { get; }
        string DebugName { get; }
    }
}