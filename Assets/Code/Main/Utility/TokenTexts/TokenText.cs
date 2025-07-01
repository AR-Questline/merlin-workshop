using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.TokenTexts {
    /// <summary>
    /// Token consists of text and child tokens.
    /// Child tokens are put into the text in format: {0}, {1}, etc.
    /// Child tokens are converted into real text in the moment of evaluation.
    /// </summary>
    public partial class TokenText {
        public virtual ushort TypeForSerialization => SavedTypes.TokenText;

        // === State
        [Saved] public TokenType Type { get; private set; }
        [Saved] public UnicodeString InputValue { get; private set; }
        [Saved] protected List<TokenText> _tokens = new();
        [Saved] protected List<TokenConverter> _converters;
        /// <summary>
        /// Used for indexing in Skill variables
        /// </summary>
        [Saved] public int AdditionalInt { get; set; }
        [Saved] public string FormatSpecifier { get; set; }

        public int NextTokenIndex => _tokens.Count;
        public IEnumerable<TokenText> Tokens => _tokens;

        bool _modified = true;
        List<object> _valuesCache = new(5);
        
        // === Static helpers
        [UnityEngine.Scripting.Preserve]
        public static string GetConverted(string input, ICharacter owner, object payload = null) => new TokenText(input).GetValue(owner, payload);

        // === Constructor
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        TokenText() {}
        
        public TokenText(TokenType type, string input, IEnumerable<TokenConverter> converters = null) {
            Type = type;
            _converters = converters?.ToList();
            InputValue = input ?? string.Empty;
        }

        public TokenText(string input, IEnumerable<TokenConverter> converters = null) : this(TokenType.PlainText, input, converters) { }
        public TokenText(TokenType type, IEnumerable<TokenConverter> converters = null) : this(type, "", converters) { }
        public TokenText(IEnumerable<TokenConverter> converters = null) : this(TokenType.PlainText, "", converters) { }

        // === Public API
        public string GetValue(ICharacter owner, object payload) {
            if (_modified) {
                Refresh();
            }
            
            try {
                _valuesCache.Clear();
                foreach (var val in _tokens) {
                    _valuesCache.Add(val.GetValue(owner, payload));
                }
                return Type.GetValue(this, owner, payload, _valuesCache);
            } catch (Exception e) {
                INamed named = payload as INamed;
                Log.Important?.Error(InputValue + " " + named?.DisplayName);
                Debug.LogException(e);
                return "";
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void ChangeTokenType(TokenType tokenType) {
            Type = tokenType;
        }
        
        public void AddToken(TokenText token, bool autoAddIndex = true) {
            if (autoAddIndex) {
                Append($"{{{NextTokenIndex.ToString()}}}");
            }
            _tokens.Add(token);
        }

        public void AddToken(TokenType type, bool autoAddIndex = true) {
            if (autoAddIndex) {
                Append($"{{{NextTokenIndex.ToString()}}}");
            }
            _tokens.Add(new TokenText(type, ""));
        }

        public void Append(string text) {
            if (text != null) {
                InputValue += text;
            }
            _modified = true;
        }

        [UnityEngine.Scripting.Preserve]
        public void RemoveToken(TokenText token) {
            if (_tokens.Contains(token)) {
                _tokens.Remove(token);
            }
        }

        public void AppendLine(string text = null) => Append($"{text ?? ""}\n");

        // === Operators
        public static implicit operator TokenText(string text) => new(text);

        // === Helpers
        /// <summary>
        /// Invoke converters resolving on the input value
        /// </summary>
        public void Refresh() {
            InputValue = TokenUtils.Construct(this, InputValue, _converters);
            _modified = false;
        }
        
        // === Equality Members

        protected bool Equals(TokenText other) {
            return Equals(Type, other.Type)
                   && InputValue == other.InputValue
                   && (_converters?.SequenceEqual(other._converters) ?? other._converters == null)
                   && _tokens.SequenceEqual(other._tokens)
                   && AdditionalInt == other.AdditionalInt
                   && FormatSpecifier == other.FormatSpecifier;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TokenText) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (_tokens != null ? _tokens.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_converters != null ? _converters.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InputValue != null ? InputValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ AdditionalInt.GetHashCode();
                hashCode = (hashCode * 397) ^ (FormatSpecifier != null ? FormatSpecifier.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}