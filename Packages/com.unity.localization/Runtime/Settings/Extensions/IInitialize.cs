namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Indicates that an extension requires an Initialization callback.
    /// </summary>
    public interface IInitialize
    {
        /// <summary>
        /// Called at the end of <see cref="LocalizationSettings.InitializationOperation"/>.
        /// </summary>
        /// <param name="settings"></param>
        void PostInitialization(LocalizationSettings settings);
    }
}
