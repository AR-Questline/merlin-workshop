using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;
using Pathfinding.WindowsStore;
using System;
using System.Linq;
#if NETFX_CORE
using WinRTLegacy;
#endif

namespace Pathfinding.Serialization {
	public class JsonMemberAttribute : System.Attribute {
	}
	public class JsonOptInAttribute : System.Attribute {
	}
	/// <summary>Indicates that the full type of the instance will always be serialized. This allows inheritance to work properly.</summary>
	public class JsonDynamicTypeAttribute : System.Attribute {
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class JsonDynamicTypeAliasAttribute : System.Attribute {
		public string alias;
		public Type type;

		public JsonDynamicTypeAliasAttribute (string alias, Type type) {
        }
    }

	// Make sure the class is not stripped out when using code stripping (see https://docs.unity3d.com/Manual/ManagedCodeStripping.html)
	[Pathfinding.Util.Preserve]
	class SerializableAnimationCurve {
		public WrapMode preWrapMode, postWrapMode;
		public Keyframe[] keys;
	}

	/// <summary>
	/// A very tiny json serializer.
	/// It is not supposed to have lots of features, it is only intended to be able to serialize graph settings
	/// well enough.
	/// </summary>
	public class TinyJsonSerializer {
		System.Text.StringBuilder output = new System.Text.StringBuilder();

		Dictionary<Type, Action<System.Object> > serializers = new Dictionary<Type, Action<object> >();

		static readonly System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

		public static void Serialize (System.Object obj, System.Text.StringBuilder output)
        {
        }

        TinyJsonSerializer()
        {
        }

        void Serialize(System.Object obj, bool serializePrivateFieldsByDefault = false)
        {
        }

        void QuotedField(string name, string contents)
        {
        }

        void SerializeUnityObject(UnityEngine.Object obj)
        {
        }
    }

    /// <summary>
    /// A very tiny json deserializer.
    /// It is not supposed to have lots of features, it is only intended to be able to deserialize graph settings
    /// well enough. Not much validation of the input is done.
    /// </summary>
    public class TinyJsonDeserializer
    {
        System.IO.TextReader reader;
		string fullTextDebug;
		GameObject contextRoot;

		static readonly System.Globalization.NumberFormatInfo numberFormat = System.Globalization.NumberFormatInfo.InvariantInfo;

		/// <summary>
		/// Deserializes an object of the specified type.
		/// Will load all fields into the populate object if it is set (only works for classes).
		/// </summary>
		public static System.Object Deserialize (string text, Type type, System.Object populate = null, GameObject contextRoot = null) {
            return default;
        }

        /// <summary>
        /// Deserializes an object of type tp.
        /// Will load all fields into the populate object if it is set (only works for classes).
        /// </summary>
        System.Object Deserialize(Type tp, System.Object populate = null)
        {
            return default;
        }

        UnityEngine.Object DeserializeUnityObject()
        {
            return default;
        }

        UnityEngine.Object DeserializeUnityObjectInner()
        {
            return default;
        }

        void EatWhitespace()
        {
        }

        void Eat(string s)
        {
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
		string EatUntil (string c, bool inString) {
            return default;
        }

        bool TryEat(char c)
        {
            return default;
        }

        string EatField()
        {
            return default;
        }

        void SkipFieldData()
        {
        }
    }
}
