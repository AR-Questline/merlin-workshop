using System;
using Awaken.Utility.Enums.Helpers;

namespace Awaken.Utility.Enums {
    /// <summary>
    /// Marker interface for sourcegen
    /// </summary>
    public interface IRichEnum { }
    
    public class RichEnum : IRichEnum, IComparable<RichEnum> {
        
        // === Properties

        public string EnumName { get; }
#if UNITY_EDITOR
        public string InspectorCategory { get; protected set; }
#endif
        
        string Serialized { get; }

        // === Constructor

        protected RichEnum(string enumName, string inspectorCategory = "") {
            EnumName = enumName;
#if UNITY_EDITOR
            InspectorCategory = inspectorCategory;
#endif
            Serialized = StaticStringSerialization.Serialize(GetType(), EnumName);
        }

        // === Queries

        public static T[] AllValuesOfType<T>() where T : RichEnum {
            return RichEnumCache.GetOnly<T>();
        }

        // === Transitioning to/from strings

        /// <summary>
        /// Retrieves the enum object from a serialized string.
        /// </summary>
        public static RichEnum Deserialize(string enumString) {
            if (enumString == null) return null;
            return StaticStringSerialization.Deserialize<RichEnum>(enumString);
        }
        
        /// <summary>
        /// Retrieves the enum object from a serialized string.
        /// </summary>
        public static T Deserialize<T>(string enumString) where T : RichEnum {
            return StaticStringSerialization.Deserialize<T>(enumString);
        }

        /// <summary>
        /// Retrieves the enum with a given name from a known type.
        /// </summary>
        public static T FromName<T>(string enumName) where T : RichEnum {
            string key = StaticStringSerialization.Serialize(typeof(T), enumName);
            return Deserialize(key) as T;
        }

        /// <summary>
        /// Serializes the enum to a string.
        /// </summary>
        public string Serialize() {
            return Serialized;
        }

        public override string ToString() => Serialize();

        public virtual int CompareTo(RichEnum other) {
            return string.Compare(EnumName, other.EnumName, StringComparison.Ordinal);
        }
    }
}
