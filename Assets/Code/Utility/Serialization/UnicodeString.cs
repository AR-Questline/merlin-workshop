using Newtonsoft.Json;

namespace Awaken.Utility.Serialization {
    /// <summary>
    /// Use this class if you want string that contains not-ascii characters.
    /// </summary>
    public class UnicodeString {
        string _value;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        UnicodeString() {}
        
        public UnicodeString(string text) {
            _value = text;
        }
        
        public override string ToString() => _value;

        public static implicit operator string(UnicodeString uni) => uni?.ToString();
        public static implicit operator UnicodeString(string text) => new(text);
    }
}