using System.Collections.Generic;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Provides access to typical character sets for various [SystemLanguages](https://docs.unity3d.com/ScriptReference/SystemLanguage.html).
    /// </summary>
    public static class TypicalCharacterSets
    {
        internal static Dictionary<SystemLanguage, char[]> s_TypicalCharacterSets = new Dictionary<SystemLanguage, char[]>
        {
            // Taken from https://www.gamasutra.com/blogs/IGDALocalizationSIG/20180504/317560/PseudoLocalization__A_Must_in_Video_Gaming
            { SystemLanguage.Czech, "áčďéěíňóřšťúůýÁČĎÉĚÍŇÓŘŠŤÚŮÝ‚„".ToCharArray() },
            { SystemLanguage.Danish, "åæøÅÆØ".ToCharArray() },
            { SystemLanguage.Dutch, "àáèéêëïóöÀÁÈÉÊËÏÓÖ".ToCharArray() },
            { SystemLanguage.Finnish, "åäöšÅÄÖŠ".ToCharArray() },
            { SystemLanguage.French, "àâæéèêëîïôœùûüçÀÂÆÉÈÊËÎÏÔŒÙÛÜÇ".ToCharArray() },
            { SystemLanguage.German, "ÄÖÜẞäöüß‚„".ToCharArray() },
            { SystemLanguage.Italian, "àéèìòùÀÉÈÌÒÙªº".ToCharArray() },
            { SystemLanguage.Norwegian, "åæøÅÆØ".ToCharArray() },
            { SystemLanguage.Polish, "ąćęłńóśżźĄĆĘŁŃÓŚŻŹ‚„".ToCharArray() },
            { SystemLanguage.Portuguese, "àáâãçéêíóôõúüÀÁÂÃÇÉÊÍÓÔÕÚÜ".ToCharArray() },
            { SystemLanguage.Russian, "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ№".ToCharArray() },
            { SystemLanguage.Spanish, "áéíóúüñÁÉÍÓÚÜÑ¿¡ªº".ToCharArray() },
            { SystemLanguage.Swedish, "åäöÅÄÖ".ToCharArray() }
        };

        /// <summary>
        /// Returns the most commonly used characters for the requested language.
        /// </summary>
        /// <param name="language">The langauge to return the most common characters for.</param>
        /// <returns>Array of the characters of null(Not all languages are supported.)</returns>
        public static char[] GetTypicalCharactersForLanguage(SystemLanguage language)
        {
            if (s_TypicalCharacterSets.TryGetValue(language, out var result))
                return result;
            return null;
        }
    }
}
