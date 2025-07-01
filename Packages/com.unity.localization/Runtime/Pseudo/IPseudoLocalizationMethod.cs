namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Provides a Pseudo-Localization transformation method as used by <see cref="PseudoLocale"/>.
    /// </summary>
    public interface IPseudoLocalizationMethod
    {
        /// <summary>
        /// Apply a Pseudo-Localization transformation to the <see cref="Message"/>.
        /// </summary>
        /// <param name="message"></param>
        void Transform(Message message);
    }
}
