#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    [Serializable]
    public class UnityObjectProperty : ITrackedPropertyValue<Object>, ISerializationCallbackReceiver
    {
        [Serializable]
        internal class LocaleIdentifierValuePair
        {
            public LocaleIdentifier localeIdentifier;
            public LazyLoadReference<Object> value;
        }

        [SerializeField] string m_PropertyPath;
        [SerializeField] string m_TypeString;
        [SerializeField] List<LocaleIdentifierValuePair> m_VariantData = new List<LocaleIdentifierValuePair>();

        internal Dictionary<LocaleIdentifier, LocaleIdentifierValuePair> m_VariantLookup = new Dictionary<LocaleIdentifier, LocaleIdentifierValuePair>();

        public string PropertyPath
        {
            get => m_PropertyPath;
            set => m_PropertyPath = value;
        }

        public Type PropertyType { get; set; }

        public bool HasVariant(LocaleIdentifier localeIdentifier) => m_VariantLookup.ContainsKey(localeIdentifier);

        public void RemoveVariant(LocaleIdentifier localeIdentifier) => m_VariantLookup.Remove(localeIdentifier);

        public bool GetValue(LocaleIdentifier localeIdentifier, out Object foundValue)
        {
            if (m_VariantLookup.TryGetValue(localeIdentifier, out var pair))
            {
                foundValue = pair.value.asset;
                return true;
            }

            foundValue = null;
            return false;
        }

        public bool GetValue(LocaleIdentifier localeIdentifier, LocaleIdentifier fallback, out Object foundValue)
        {
            if (m_VariantLookup.TryGetValue(localeIdentifier, out var pair) || m_VariantLookup.TryGetValue(fallback, out pair))
            {
                foundValue = pair.value.asset;
                return true;
            }

            foundValue = null;
            return false;
        }

        public void SetValue(LocaleIdentifier localeIdentifier, Object newValue)
        {
            if (!m_VariantLookup.TryGetValue(localeIdentifier, out var variantData))
            {
                variantData = new LocaleIdentifierValuePair { localeIdentifier = localeIdentifier };
                m_VariantLookup[localeIdentifier] = variantData;
            }

            variantData.value.asset = newValue;
        }

        public void OnBeforeSerialize()
        {
            m_TypeString = PropertyType?.AssemblyQualifiedName;
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

            if (!string.IsNullOrEmpty(m_TypeString))
                PropertyType = Type.GetType(m_TypeString);
        }
    }
}

#endif
