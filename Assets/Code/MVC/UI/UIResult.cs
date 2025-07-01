namespace Awaken.TG.MVC.UI {
    /// <summary>
    /// Returned from event handler to inform the UI system what was done about the event.
    /// </summary>
    public enum UIResult : byte {
        /// <summary>
        /// This handler wasn't interest in that event at all.
        /// </summary>
        Ignore, 
        /// <summary>
        /// The event was accepted - don't propagate it further down the
        /// interaction stack and perform default behavior (if any).
        /// </summary>
        Accept, 
        /// <summary>
        /// The event is prevented - don't propagate it down the interaction stack,
        /// don't perform any further handling.
        /// </summary>
        Prevent, 
    }
}