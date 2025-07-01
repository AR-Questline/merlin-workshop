namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Reverses all strings, to simulate right-to-left locales.
    /// </summary>
    public class Mirror : IPseudoLocalizationMethod
    {
        /// <summary>
        /// Reverse all strings, to simulate right-to-left locales.
        /// </summary>
        /// <param name="message"></param>
        public void Transform(Message message)
        {
            foreach (var fragment in message.Fragments)
            {
                if (fragment is WritableMessageFragment writableMessageFragment)
                    MirrorFragment(writableMessageFragment);
            }
        }

        void MirrorFragment(WritableMessageFragment writableMessageFragment)
        {
            var mirrorBuffer = new char[writableMessageFragment.Length];

            int readPos = writableMessageFragment.Length - 1;
            int writePos;

            // We search for a new line char in reverse,
            // when we find one we then copy that line into the buffer in reverse.
            for (int i = writableMessageFragment.Length - 1; i >= 0; --i)
            {
                // Look for a new line
                if (writableMessageFragment[i] == '\n')
                {
                    // Add the new line char
                    mirrorBuffer[i] = '\n';

                    // Mirror the line after the new line char.
                    writePos = i + 1;
                    while (readPos > i)
                    {
                        mirrorBuffer[writePos++] = writableMessageFragment[readPos--];
                    }
                    readPos = i - 1;
                }
            }

            // Copy the remainder
            writePos = 0;
            while (readPos >= 0)
            {
                mirrorBuffer[writePos++] = writableMessageFragment[readPos--];
            }

            writableMessageFragment.Text = new string(mirrorBuffer);
        }
    }
}
