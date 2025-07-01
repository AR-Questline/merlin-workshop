#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;
using UnityEngine.Localization.SmartFormat;

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    /// <summary>
    /// Interface for accessing a tracked property`s values.
    /// </summary>
    /// <typeparam name="T">The property data type.</typeparam>
    public interface ITrackedPropertyValue<T> : ITrackedProperty
    {
        /// <summary>
        /// Attempts to find a value for the provided LocaleIdentifier.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier to check for.</param>
        /// <param name="foundValue">The value found for the LocaleIdentifier.</param>
        /// <returns><see langword="true"/> if a value was found and <see langword="false"/> if one was not.</returns>
        bool GetValue(LocaleIdentifier localeIdentifier, out T foundValue);

        /// <summary>
        /// Attempts to find a value for the provided LocaleIdentifier or fallback.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier to check for.</param>
        /// <param name="fallback">The LocaleIdentifier to fallback to if one could not be found for <paramref name="localeIdentifier"/>.</param>
        /// <param name="foundValue">The value found for the LocaleIdentifier or fallback.</param>
        /// <returns><see langword="true"/> if a value was found and <see langword="false"/> if one was not.</returns>
        bool GetValue(LocaleIdentifier localeIdentifier, LocaleIdentifier fallback, out T foundValue);

        /// <summary>
        /// Assigns a value for the chosen LocaleIdentifier.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier the variant should be applied to</param>
        /// <param name="value">The variant value for the LocaleIdentifier.</param>
        void SetValue(LocaleIdentifier localeIdentifier, T value);
    }

    internal interface ITrackedPropertyRemoveVariant
    {
        void RemoveVariant(LocaleIdentifier localeIdentifier);
    }

    /// <summary>
    /// Represents a property for a primitive data type.
    /// </summary>
    /// <typeparam name="TPrimitive">The primitive data type.</typeparam>
    [Serializable]
    public class TrackedProperty<TPrimitive> : ITrackedPropertyValue<TPrimitive>, IStringProperty, ISerializationCallbackReceiver, ITrackedPropertyRemoveVariant
    {
        [Serializable]
        internal class LocaleIdentifierValuePair
        {
            public LocaleIdentifier localeIdentifier;
            public TPrimitive value;
        }

        [SerializeField] string m_PropertyPath;
        [SerializeField] List<LocaleIdentifierValuePair> m_VariantData = new List<LocaleIdentifierValuePair>();

        internal Dictionary<LocaleIdentifier, LocaleIdentifierValuePair> m_VariantLookup = new Dictionary<LocaleIdentifier, LocaleIdentifierValuePair>();

        /// <summary>
        /// The property's serialized property path.
        /// </summary>
        public string PropertyPath
        {
            get => m_PropertyPath;
            set => m_PropertyPath = value;
        }

        public bool HasVariant(LocaleIdentifier localeIdentifier) => m_VariantLookup.ContainsKey(localeIdentifier);

        public void RemoveVariant(LocaleIdentifier localeIdentifier) => m_VariantLookup.Remove(localeIdentifier);

        public bool GetValue(LocaleIdentifier localeIdentifier, out TPrimitive foundValue)
        {
            if (m_VariantLookup.TryGetValue(localeIdentifier, out var pair))
            {
                foundValue = pair.value;
                return true;
            }

            foundValue = default;
            return false;
        }

        public bool GetValue(LocaleIdentifier localeIdentifier, LocaleIdentifier fallback, out TPrimitive foundValue)
        {
            if (m_VariantLookup.TryGetValue(localeIdentifier, out var pair) || m_VariantLookup.TryGetValue(fallback, out pair))
            {
                foundValue = pair.value;
                return true;
            }

            foundValue = default;
            return false;
        }

        public void SetValue(LocaleIdentifier localeIdentifier, TPrimitive value)
        {
            if (!m_VariantLookup.TryGetValue(localeIdentifier, out var pair))
            {
                pair = new LocaleIdentifierValuePair { localeIdentifier = localeIdentifier };
                m_VariantLookup[localeIdentifier] = pair;
            }

            pair.value = value;
        }

        public string GetValueAsString(LocaleIdentifier localeIdentifier) => GetValue(localeIdentifier, out var foundValue) ? ConvertToString(foundValue) : null;

        public string GetValueAsString(LocaleIdentifier localeIdentifier, LocaleIdentifier fallback) => GetValue(localeIdentifier, fallback, out var foundValue) ? ConvertToString(foundValue) : null;

        public void SetValueFromString(LocaleIdentifier localeIdentifier, string stringValue)
        {
            var convertedValue = ConvertFromString(stringValue);
            SetValue(localeIdentifier, convertedValue);
        }

        protected virtual string ConvertToString(TPrimitive value) => Convert.ToString(value);
        protected virtual TPrimitive ConvertFromString(string value) => (TPrimitive)Convert.ChangeType(value, typeof(TPrimitive));

        public void OnBeforeSerialize()
        {
            m_VariantData.Clear();
            foreach (var pair in m_VariantLookup.Values)
            {
                m_VariantData.Add(pair);
            }
        }

        public void OnAfterDeserialize()
        {
            m_VariantLookup.Clear();
            foreach (var pair in m_VariantData)
            {
                m_VariantLookup[pair.localeIdentifier] = pair;
            }
        }

        public override string ToString() => Smart.Format("{GetType().Name}({PropertyPath}) - {1:list:{Key}({Value.value})|, |, }", this, m_VariantLookup);
    }

    [Serializable] public class ByteTrackedProperty : TrackedProperty<byte> {}
    [Serializable] public class SByteTrackedProperty : TrackedProperty<sbyte> {}
    [Serializable] public class CharTrackedProperty : TrackedProperty<char> {}
    [Serializable] public class ShortTrackedProperty : TrackedProperty<short> {}
    [Serializable] public class UShortTrackedProperty : TrackedProperty<ushort> {}
    [Serializable] public class IntTrackedProperty : TrackedProperty<int> {}
    [Serializable] public class UIntTrackedProperty : TrackedProperty<uint> {}
    [Serializable] public class LongTrackedProperty : TrackedProperty<long> {}
    [Serializable] public class ULongTrackedProperty : TrackedProperty<ulong> {}
    [Serializable] public class FloatTrackedProperty : TrackedProperty<float> {}
    [Serializable] public class DoubleTrackedProperty : TrackedProperty<double> {}
    [Serializable] public class ArraySizeTrackedProperty : UIntTrackedProperty {}

    // Same as Int but we use a custom property drawer which resolves the Enum type in the inspector.
    [Serializable] public class EnumTrackedProperty : IntTrackedProperty {}

    [Serializable]
    public class BoolTrackedProperty : TrackedProperty<bool>
    {
        protected override bool ConvertFromString(string value)
        {
            // Support bool in the form "0" or "1".
            if (int.TryParse(value, out var result))
                return (bool)Convert.ChangeType(result, typeof(bool));

            // Support bool in the form "false" or "true".
            return base.ConvertFromString(value);
        }

        protected override string ConvertToString(bool value)
        {
            return value ? "1" : "0";
        }
    }

    [Serializable]
    public class StringTrackedProperty : TrackedProperty<string>
    {
        protected override string ConvertFromString(string value) => value;
        protected override string ConvertToString(string value) => value;
    }
}

#endif
