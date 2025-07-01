#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    /// <summary>
    /// Provides localization support for a tracked string property.
    /// </summary>
    [Serializable]
    public class LocalizedStringProperty : ITrackedProperty
    {
        [SerializeField]
        LocalizedString m_Localized = new LocalizedString();

        [SerializeField]
        string m_PropertyPath;

        /// <summary>
        /// The Localized String that will be used for this tracked property.
        /// </summary>
        public LocalizedString LocalizedString
        {
            get => m_Localized;
            set => m_Localized = value;
        }

        public string PropertyPath
        {
            get => m_PropertyPath;
            set => m_PropertyPath = value;
        }

        public bool HasVariant(LocaleIdentifier localeIdentifier)
        {
            if (LocalizedString.IsEmpty)
                return false;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return true;
            }
            #endif

            return false;
        }
    }
}

#endif
