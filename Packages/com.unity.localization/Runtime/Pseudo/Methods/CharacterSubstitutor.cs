using System;
using System.Collections.Generic;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Replaces characters in the input string.
    /// </summary>
    [Serializable]
    public class CharacterSubstitutor : IPseudoLocalizationMethod, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The substitution method to use when applying Pseudo-Localization.
        /// </summary>
        public enum SubstitutionMethod
        {
            /// <summary>
            /// Converts all characters to uppercase.
            /// </summary>
            ToUpper,

            /// <summary>
            /// Converts all characters to lowercase.
            /// </summary>
            ToLower,

            /// <summary>
            /// Replaces all characters with a single character from <see cref="ReplacementList"/>.
            /// This method is a good way to find any hard coded text or to test for missing characters in a font.
            /// </summary>
            List,

            /// <summary>
            /// Uses <see cref="ReplacementMap"/> to replace each character, when it is present in the dictionary.
            /// </summary>
            Map
        }

        [Serializable]
        struct CharReplacement
        {
            public char original;
            public char replacement;
        }

        /// <summary>
        /// The method to use when selecting a character from <see cref="ReplacementList"/>.
        /// </summary>
        public enum ListSelectionMethod
        {
            /// <summary>
            /// Select characters at random from <see cref="ReplacementList"/>.
            /// </summary>
            Random,

            /// <summary>
            /// Starts at the position from the previous input string and use each character in <see cref="ReplacementList"/>,
            /// looping back to the start if the end of the list is reached.
            /// </summary>
            LoopFromPrevious,

            /// <summary>
            /// Starts at the first item in <see cref="ReplacementList"/> and uses each character in the list
            /// looping back to the start if the end of the list is reached.
            /// </summary>
            LoopFromStart
        }

        [SerializeField]
        SubstitutionMethod m_SubstitutionMethod;

        [SerializeField]
        ListSelectionMethod m_ListMode;

        [SerializeField]
        List<CharReplacement> m_ReplacementsMap;

        [SerializeField]
        List<char> m_ReplacementList = new List<char> { '_' };

        internal int m_ReplacementsPosition;

        /// <summary>
        /// The substitution method to use.
        /// </summary>
        public SubstitutionMethod Method
        {
            get => m_SubstitutionMethod;
            set => m_SubstitutionMethod = value;
        }

        /// <summary>
        /// Dictionary of characters that will be replaced when using <see cref="SubstitutionMethod.Map"/>. Any value that is not present will be preserved.
        /// </summary>
        public Dictionary<char, char> ReplacementMap { get; private set; } = new Dictionary<char, char>();

        /// <summary>
        /// The method to use when selecting a character from <see cref="ReplacementList"/>.
        /// </summary>
        public ListSelectionMethod ListMode
        {
            get => m_ListMode;
            set => m_ListMode = value;
        }

        /// <summary>
        /// The characters to use when using <see cref="SubstitutionMethod.List"/> mode.
        /// </summary>
        public List<char> ReplacementList => m_ReplacementList;

        int GetRandomSeed(string input) => input.GetHashCode();

        /// <summary>
        /// Attempts to find a replacement character and return it.
        /// </summary>
        /// <param name="value">The character to replace.</param>
        /// <returns>The replacement character of the same if none could be found.</returns>
        internal char ReplaceCharFromMap(char value)
        {
            if (ReplacementMap != null && ReplacementMap.TryGetValue(value, out char replacement))
                return replacement;
            return value;
        }

        public void OnBeforeSerialize()
        {
            if (m_ReplacementsMap == null)
                m_ReplacementsMap = new List<CharReplacement>();
            m_ReplacementsMap.Clear();

            foreach (var pair in ReplacementMap)
            {
                m_ReplacementsMap.Add(new CharReplacement() { original = pair.Key, replacement = pair.Value });
            }
        }

        public void OnAfterDeserialize()
        {
            if (ReplacementMap == null)
                ReplacementMap = new Dictionary<char, char>();
            ReplacementMap.Clear();

            foreach (var d in m_ReplacementsMap)
            {
                ReplacementMap[d.original] = d.replacement;
            }
        }

        void TransformFragment(WritableMessageFragment writableFragment)
        {
            switch (Method)
            {
                case SubstitutionMethod.Map:
                    var converted = new char[writableFragment.Length];
                    for (int j = 0; j < converted.Length; ++j)
                    {
                        converted[j] = ReplaceCharFromMap(writableFragment[j]);
                    }
                    writableFragment.Text = new string(converted);
                    break;

                case SubstitutionMethod.ToUpper:
                    writableFragment.Text = writableFragment.Text.ToUpper();
                    break;

                case SubstitutionMethod.ToLower:
                    writableFragment.Text = writableFragment.Text.ToLower();
                    break;

                case SubstitutionMethod.List:

                    if (m_ReplacementList == null || m_ReplacementList.Count == 0)
                        break;

                    if (m_ReplacementList.Count == 1)
                    {
                        writableFragment.Text = new string(m_ReplacementList[0], writableFragment.Length);
                        break;
                    }

                    var newValues = new char[writableFragment.Length];

                    if (ListMode == ListSelectionMethod.Random)
                    {
                        Random.InitState(GetRandomSeed(writableFragment.Message.Original));
                        for (int i = 0; i < newValues.Length; ++i)
                        {
                            newValues[i] = m_ReplacementList[Random.Range(0, m_ReplacementList.Count)];
                        }
                    }
                    else
                    {
                        if (ListMode == ListSelectionMethod.LoopFromStart)
                            m_ReplacementsPosition = 0;

                        for (int i = 0; i < newValues.Length; ++i, ++m_ReplacementsPosition)
                        {
                            newValues[i] = m_ReplacementList[m_ReplacementsPosition % m_ReplacementList.Count];
                        }
                    }
                    writableFragment.Text = new string(newValues);
                    break;
            }
        }

        /// <summary>
        /// Replaces each character in the input with a replacement character if one can be found.
        /// If a replacement character can not be found then the original is kept.
        /// </summary>
        /// <param name="message"></param>
        public void Transform(Message message)
        {
            foreach (var fragment in message.Fragments)
            {
                if (fragment is WritableMessageFragment writableFragment)
                    TransformFragment(writableFragment);
            }
        }
    }
}
