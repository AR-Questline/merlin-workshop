#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    /// <summary>
    /// Provides localization support to tracked [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html).
    /// </summary>
    [Serializable]
    public class LocalizedAssetProperty : ITrackedProperty
    {
        [SerializeReference] LocalizedAssetBase m_Localized;
        [SerializeField] string m_PropertyPath;

        /// <summary>
        /// The Localized Object that will be used for this tracked property.
        /// </summary>
        public LocalizedAssetBase LocalizedObject
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
            if (LocalizedObject.IsEmpty)
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
