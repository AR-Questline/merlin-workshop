using System;
using UnityEngine.Pool;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Provides a pseudo-localization method to preserve certain parts of a string and prevent them from being modified, such as Rich Text tags.
    /// The method works by identifying text that is contained between an opening and closing tag and marking it as a <see cref="ReadOnlyMessageFragment"/>.
    /// </summary>
    [Serializable]
    public class PreserveTags : IPseudoLocalizationMethod
    {
        [SerializeField]
        char m_Opening = '<';

        [SerializeField]
        char m_Closing = '>';

        /// <summary>
        /// The opening tag used to identify when to preserve text.
        /// </summary>
        public char Opening
        {
            get => m_Opening;
            set => m_Opening = value;
        }

        /// <summary>
        /// The closing tag used to identify when to preserve text.
        /// </summary>
        public char Closing
        {
            get => m_Closing;
            set => m_Closing = value;
        }

        /// <inheritdoc/>
        public void Transform(Message message)
        {
            using (ListPool<MessageFragment>.Get(out var messageFragments))
            {
                for (int i = 0; i < message.Fragments.Count; ++i)
                {
                    int startTextBlockIdx = 0;
                    int lastOpeningBrackedIdx = -1;
                    var fragment = message.Fragments[i];
                    if (fragment is WritableMessageFragment writableMessage)
                    {
                        for (int j = 0; j < fragment.Length; ++j)
                        {
                            if (fragment[j] == m_Opening)
                            {
                                lastOpeningBrackedIdx = j;
                            }
                            else if (fragment[j] == m_Closing && lastOpeningBrackedIdx != -1)
                            {
                                var closingIdx = j + 1;

                                // Create a fragment for any text before the bracket
                                if (startTextBlockIdx != lastOpeningBrackedIdx)
                                {
                                    messageFragments.Add(writableMessage.CreateTextFragment(startTextBlockIdx, lastOpeningBrackedIdx));
                                }

                                messageFragments.Add(writableMessage.CreateReadonlyTextFragment(lastOpeningBrackedIdx, closingIdx));
                                lastOpeningBrackedIdx = -1;
                                startTextBlockIdx = j + 1;
                            }
                        }

                        // Release the original fragment
                        message.ReleaseFragment(fragment);

                        if (startTextBlockIdx != fragment.Length)
                            messageFragments.Add(writableMessage.CreateTextFragment(startTextBlockIdx, fragment.Length));
                    }
                    else
                    {
                        messageFragments.Add(fragment);
                    }
                }

                message.Fragments.Clear();
                message.Fragments.AddRange(messageFragments);
            }
        }
    }
}
