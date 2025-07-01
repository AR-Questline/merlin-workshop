namespace Awaken.TG.Main.Utility.Semaphores {
    /// <summary>
    /// Object handling callbacks of any semaphore.
    /// </summary>
    public interface ISemaphoreObserver {
        internal void OnUp() { }
        internal void OnDown() { }
        internal void OnStateChanged(bool newState) { }
    }
}