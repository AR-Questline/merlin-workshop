namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Provides a reset callback which can be used to reset the internal state when exiting edit and play mode.
    /// </summary>
    public interface IReset
    {
        /// <summary>
        /// Resets the internal state of the object so it is ready to be used again and does not contain any data left over from a previous run.
        /// </summary>
        void ResetState();
    }
}
