using System;
using System.Collections.Generic;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Expands the size of the input string to show where there may not be enough space for languages that result in longer strings, and either wrap awkwardly or truncate.
    /// </summary>
    [Serializable]
    public class Expander : IPseudoLocalizationMethod
    {
        /// <summary>
        /// Where to insert the padding characters.
        /// </summary>
        public enum InsertLocation
        {
            /// <summary>
            /// At the beginning of the input string.
            /// </summary>
            Start,

            /// <summary>
            /// At the end of the input string.
            /// </summary>
            End,

            /// <summary>
            /// Split between the beginning and end of the input string.
            /// </summary>
            Both
        }

        /// <summary>
        /// A rule used to determine how much the string length should be increased by.
        /// </summary>
        [Serializable]
        public struct ExpansionRule : IComparable<ExpansionRule>
        {
            [SerializeField]
            int m_MinCharacters;

            [SerializeField]
            int m_MaxCharacters;

            [SerializeField]
            float m_ExpansionAmount;

            /// <summary>
            /// The minimum characters. The evaluated string length must be equal or greater than this value.
            /// </summary>
            public int MinCharacters
            {
                get => m_MinCharacters;
                set => m_MinCharacters = Mathf.Max(0, value);
            }

            /// <summary>
            /// The maximum characters. The evaluated string length must be less than this value.
            /// </summary>
            public int MaxCharacters
            {
                get => m_MaxCharacters;
                set => m_MaxCharacters = Mathf.Max(0, value);
            }

            /// <summary>
            /// The amount to increase the string length by as a ratio where 0 is no expansion and 1.0 is 100%(double length).
            /// It varies per language but 0.3 is a good value when using English as the source language.
            /// </summary>
            public float ExpansionAmount
            {
                get => m_ExpansionAmount;
                set => m_ExpansionAmount = Mathf.Max(0, value);
            }

            /// <summary>
            /// Create a new Expansion Rule instance.
            /// </summary>
            /// <param name="minCharacters"></param>
            /// <param name="maxCharacters"></param>
            /// <param name="expansion"></param>
            public ExpansionRule(int minCharacters, int maxCharacters, float expansion)
            {
                m_MinCharacters = Mathf.Max(0, minCharacters);
                m_MaxCharacters = Mathf.Max(0, maxCharacters);
                m_ExpansionAmount = Mathf.Max(0, expansion);
            }

            internal bool InRange(int length)
            {
                // We only check less than(not equal) for the max value so we can specify ranges like 0-10, 10-20 etc.
                return length >= MinCharacters && length < MaxCharacters;
            }

            /// <summary>
            /// Used for sorting the expansion rules. Rules are sorted by the <see cref="MinCharacters"/> value.
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public int CompareTo(ExpansionRule other) => MinCharacters.CompareTo(other.MinCharacters);
        }

        [SerializeField]
        List<ExpansionRule> m_ExpansionRules = new List<ExpansionRule>
        {
            // Default values based on IBM `Guidelines to design global solutions`.
            new ExpansionRule(0, 10, 2),
            new ExpansionRule(10, 20, 1),
            new ExpansionRule(20, 30, 0.8f),
            new ExpansionRule(30, 50, 0.6f),
            new ExpansionRule(50, 70, 0.7f),
            new ExpansionRule(70, int.MaxValue, 0.3f)
        };

        [SerializeField]
        InsertLocation m_Location = InsertLocation.End;

        [SerializeField]
        int m_MinimumStringLength = 1;

        [SerializeField]
        List<char> m_PaddingCharacters = new List<char>();

        /// <summary>
        /// Rules based on string length, that determine the amount to append onto the input string as a ratio of its length.
        /// For example, 0.3 would add an extra 30% onto the length.
        /// Note: Negative values are ignored.
        /// When the newly calculated length is not whole then the next largest whole number will be used.
        /// Rules can also be added using <see cref="SetConstantExpansion"/> and <see cref="AddExpansionRule(int, int, float)"/>.
        /// </summary>
        public List<ExpansionRule> ExpansionRules => m_ExpansionRules;

        /// <summary>
        /// The location where the padding characters will be added to the input string.
        /// </summary>
        public InsertLocation Location
        {
            get => m_Location;
            set => m_Location = value;
        }

        /// <summary>
        /// The characters to randomly pick from when padding the length.
        /// </summary>
        public List<char> PaddingCharacters => m_PaddingCharacters;

        /// <summary>
        /// The minimum length strings should be before evaluating the <see cref="ExpansionRules"/>.
        /// For example if the value was 10, then all strings under 10 in length would be increased to 10 before the expansion rules were applied.
        /// By default this value is 1.
        /// </summary>
        public int MinimumStringLength
        {
            get => m_MinimumStringLength;
            set => m_MinimumStringLength = Mathf.Max(0, value);
        }

        /// <summary>
        /// Creates an instance with default padding characters.
        /// </summary>
        public Expander()
        {
            AddCharacterRange('!', '~');
        }

        /// <summary>
        /// Creates an instance with a single padding character.
        /// </summary>
        /// <param name="paddingCharacter">The character to use for padding.</param>
        public Expander(char paddingCharacter)
        {
            PaddingCharacters.Add(paddingCharacter);
        }

        /// <summary>
        /// Creates an instance with a range of padding characters from start to end.
        /// </summary>
        /// <param name="start">The character at the start of the range.</param>
        /// <param name="end">The last character in the range.</param>
        public Expander(char start, char end)
        {
            AddCharacterRange(start, end);
        }

        /// <summary>
        /// Adds all characters between start and end to the list of padding characters.
        /// </summary>
        /// <param name="start">Character to start with.</param>
        /// <param name="end">Last character to add.</param>
        public void AddCharacterRange(char start, char end)
        {
            for (var i = start; i < end; ++i)
            {
                PaddingCharacters.Add(i);
            }
        }

        /// <summary>
        /// Sets a single expansion rule that will be applied to all strings.
        /// </summary>
        /// <param name="expansion"></param>
        public void SetConstantExpansion(float expansion)
        {
            if (m_ExpansionRules != null)
                m_ExpansionRules.Clear();
            AddExpansionRule(0, int.MaxValue, expansion);
        }

        /// <summary>
        /// Adds an expansion rule to <see cref="ExpansionRules"/>
        /// </summary>
        /// <param name="minCharacters">The minimum characters. The evaluated string length must be equal or greater than this value.</param>
        /// <param name="maxCharacters">The maximum characters. The evaluated string length must be less than this value.</param>
        /// <param name="expansion">The amount to increase the string length by as a ratio where 0 is no expansion and 1.0 is 100%(double length). It varies per language but 0.3 is a good value when using English.</param>
        public void AddExpansionRule(int minCharacters, int maxCharacters, float expansion)
        {
            if (m_ExpansionRules == null)
                m_ExpansionRules = new List<ExpansionRule>();
            m_ExpansionRules.Add(new ExpansionRule(minCharacters, maxCharacters, expansion));
        }

        internal float GetExpansionForLength(int length)
        {
            foreach (var item in ExpansionRules)
            {
                if (item.InRange(length))
                    return item.ExpansionAmount;
            }
            return 0;
        }

        /// <summary>
        /// Pad the string with random characters to increase its length.
        /// </summary>
        /// <param name="message"></param>
        public void Transform(Message message)
        {
            var messageLength = message.Length;
            int stringLength = Mathf.Max(messageLength, MinimumStringLength);
            var paddingAmount = Mathf.CeilToInt(GetExpansionForLength(stringLength) * stringLength);
            if (paddingAmount > 0)
            {
                // Add the extra length which may have resulted due to the minimum string length.
                paddingAmount += stringLength - messageLength;

                var padding = new char[paddingAmount];
                Random.InitState(GetRandomSeed(message.Original));

                for (int i = 0; i < paddingAmount; ++i)
                {
                    padding[i] = PaddingCharacters[Random.Range(0, PaddingCharacters.Count)];
                }

                AddPaddingToMessage(message, padding);
            }
        }

        void AddPaddingToMessage(Message message, char[] padding)
        {
            MessageFragment start = null;
            MessageFragment end = null;
            string paddingString = new string(padding);

            if (Location == InsertLocation.Start)
                start = message.CreateTextFragment(paddingString);
            else if (Location == InsertLocation.End)
                end = message.CreateTextFragment(paddingString);
            else // Both
            {
                int splitPoint = Mathf.FloorToInt(padding.Length * 0.5f);
                start = message.CreateTextFragment(paddingString, 0, splitPoint);
                end = message.CreateTextFragment(paddingString, splitPoint, padding.Length - 1);
            }

            if (start != null)
                message.Fragments.Insert(0, start);
            if (end != null)
                message.Fragments.Add(end);
        }

        int GetRandomSeed(string input) => input.GetHashCode();
    }
}
