namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Provides a way to generate unique Id values for table entries.
    /// </summary>
    public interface IKeyGenerator
    {
        /// <summary>
        /// Return the next Id value that can be used.
        /// </summary>
        /// <returns></returns>
        long GetNextKey();
    }
}
