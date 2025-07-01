using System.Collections.Generic;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// The PseudoLocale can be added to a project to enable Pseudo-localization.
    /// Pseudo-localization is a method for testing internationalization aspects of your game.
    /// Pseudo-localization can help highlight issues that may occur when translating into different
    /// languages. For example, it can help to highlight areas where there may not be enough space for
    /// translated text or where the text is hard coded or built in a way that will be difficult to
    /// localize(word order).
    ///
    /// Typically pseudo-localization can be used to highlight the following issues:
    /// <list type="bullet">
    /// <item>
    /// <description>
    ///     Text length that is significantly longer than the source language, and does not fit within
    ///     the UI constraints, or which causes text breaks at awkward positions.
    ///     On average, expect an English string to increase in length by 30% when translating to another
    ///     language.
    ///     See <see cref="Expander"/>, <see cref="Encapsulator"/>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    ///     Vertical height issues caused by Font glyphs that are significantly larger than, or possess
    ///     diacritic marks not found in, the source language, and which may be cut off vertically.
    ///     See <see cref="Accenter"/>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    ///     Right-to-left issues from languages for which the reading order is not left-to-right, which is
    ///     especially problematic for user input.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    ///     Missing Font characters. Some fonts may have a limited character set that will not accommodate non
    ///     ASCII character sets. See <see cref="CharacterSubstitutor"/>, <see cref="Accenter"/>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    ///     Word order issues. Text that is concatenated from multiple strings will often assume the words
    ///     will always follow the same order, however when localized the order may change.
    ///     Pseudo-localization can highlight where text has been constructed in this manner.
    ///     See <see cref="Encapsulator"/>
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(menuName = "Localization/Pseudo-Locale", fileName = "Pseudo-Locale(pseudo)")]
    public class PseudoLocale : Locale
    {
        [SerializeReference]
        List<IPseudoLocalizationMethod> m_Methods = new List<IPseudoLocalizationMethod>
        {
            new PreserveTags(),
            new Expander(),
            new Accenter(),
            new Encapsulator()
        };

        /// <summary>
        /// The pseudo localization methods that will be applied to the source text.
        /// </summary>
        public List<IPseudoLocalizationMethod> Methods => m_Methods;

        /// <summary>
        /// Create a new instance with default values.
        /// </summary>
        /// <returns></returns>
        public static PseudoLocale CreatePseudoLocale()
        {
            var locale = CreateInstance<PseudoLocale>();
            locale.name = nameof(PseudoLocale);
            return locale;
        }

        PseudoLocale()
        {
            Identifier = new LocaleIdentifier("en");
        }

        internal void Reset()
        {
            foreach (var method in Methods)
            {
                if (method is CharacterSubstitutor cs)
                {
                    cs.m_ReplacementsPosition = 0;
                }
            }
        }

        /// <summary>
        /// Returns a pseudo-localized string using <see cref="Methods"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual string GetPseudoString(string input)
        {
            var message = Message.CreateMessage(input);
            foreach (var method in Methods)
            {
                method?.Transform(message);
            }

            var result = message.ToString();
            message.Release();
            return result;
        }

        public override string ToString() => $"Pseudo ({base.ToString()})";
    }
}
