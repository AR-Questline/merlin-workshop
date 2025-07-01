using System;
using Awaken.Utility.Extensions;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    /// <summary>
    /// Copied from <see cref="RawSuggestion"/> and modified to support nicifying searched names
    /// </summary>
    public class SimplifiedSuggestion : IQcSuggestion {
        readonly string _value;
        readonly bool _singleLiteral;
        readonly string _completion;

        public string FullSignature => _completion;
        public string PrimarySignature => _value;
        public string SecondarySignature => string.Empty;

        /// <summary>
        /// Constructs a suggestion from the provided value.
        /// </summary>
        /// <param name="value">The value to suggest.</param>
        /// <param name="singleLiteral">If the value should be treated as a single literal then "" will be used as necessary.</param>
        /// <param name="toRemove"></param>
        public SimplifiedSuggestion(string value, bool singleLiteral = false, string removeAllInfront = null, params string[] toRemove) {
            _value = value;
            _singleLiteral = singleLiteral;
            _completion = _value;

            if (!removeAllInfront.IsNullOrWhitespace()) {
                int indexOf = _value.IndexOf(removeAllInfront, StringComparison.Ordinal);
                if (indexOf != -1) {
                    _value = _value.Remove(0, indexOf + removeAllInfront.Length);
                }
            }
            
            for (int i = 0; i < toRemove.Length; i++) {
                _value = _value.Replace(toRemove[i], string.Empty);
            }

            _value = _value.Replace("_", " ");
        }

        public bool MatchesPrompt(string prompt) {
            if (_singleLiteral) {
                prompt = prompt.Trim('"');
            }

            return prompt == _value || prompt == _completion;
        }

        public string GetCompletion(string prompt) {
            return _completion;
        }

        public string GetCompletionTail(string prompt) {
            return string.Empty;
        }

        public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context) {
            return null;
        }
    }
}