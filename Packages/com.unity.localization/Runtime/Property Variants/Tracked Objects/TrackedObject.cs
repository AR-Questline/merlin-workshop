#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Provides common Property Variant functionality for a Unity object.
    /// You can inherit from this class to create custom object trackers.
    /// <example>
    /// This shows how to create a <see cref="TrackedObjects"/> to support the <see cref="AudioSource"/> component.
    /// <code source="../../../DocCodeSamples.Tests/GameObjectLocalizerSamples.cs" region="custom-audio"/>
    /// </example>
    /// </summary>
    [Serializable]
    public abstract class TrackedObject : ISerializationCallbackReceiver
    {
        // Class so we can provide a custom collection PropertyDrawer
        [Serializable]
        internal class TrackedPropertiesCollection
        {
            [SerializeReference]
            public List<ITrackedProperty> items = new List<ITrackedProperty>();
        }

        [SerializeField, HideInInspector]
        Object m_Target;

        [SerializeField]
        TrackedPropertiesCollection m_TrackedProperties = new TrackedPropertiesCollection();

        readonly Dictionary<string, ITrackedProperty> m_PropertiesLookup = new Dictionary<string, ITrackedProperty>();

        /// <summary>
        /// The target that the variants will be applied to.
        /// </summary>
        public Object Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        /// <summary>
        /// The tracked properties for this object.
        /// </summary>
        public IList<ITrackedProperty> TrackedProperties => m_TrackedProperties.items;

        /// <summary>
        /// Can be used to reject certain properties.
        /// </summary>
        /// <param name="propertyPath"></param>
        /// <returns></returns>
        public virtual bool CanTrackProperty(string propertyPath) => true;

        /// <summary>
        /// Create and add a tracked property for this object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyPath"></param>
        /// <returns></returns>
        public T AddTrackedProperty<T>(string propertyPath) where T : ITrackedProperty, new()
        {
            var property = new T { PropertyPath = propertyPath };
            AddTrackedProperty(property);
            return property;
        }

        /// <summary>
        /// Add a tracked property for this object.
        /// </summary>
        /// <param name="trackedProperty"></param>
        public virtual void AddTrackedProperty(ITrackedProperty trackedProperty)
        {
            if (trackedProperty == null)
                throw new ArgumentNullException(nameof(trackedProperty));

            if (string.IsNullOrEmpty(trackedProperty.PropertyPath))
                throw new ArgumentException("Property path must not be null or empty.");

            if (m_PropertiesLookup.ContainsKey(trackedProperty.PropertyPath))
                throw new ArgumentException(trackedProperty.PropertyPath + " is already tracked.");

            m_PropertiesLookup[trackedProperty.PropertyPath] = trackedProperty;
            TrackedProperties.Add(trackedProperty);
        }

        /// <summary>
        /// Remove a tracked property for this object.
        /// </summary>
        /// <param name="trackedProperty">The tracked property to be removed.</param>
        /// <returns>Returns <see langword="true"/> if the value was removed; otherwise <see langword="false"/>.</returns>
        public virtual bool RemoveTrackedProperty(ITrackedProperty trackedProperty)
        {
            m_PropertiesLookup.Remove(trackedProperty.PropertyPath);
            return TrackedProperties.Remove(trackedProperty);
        }

        /// <summary>
        /// Return the tracked property for the property path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyPath">The serialized property path.</param>
        /// <param name="create">Specify whether to create a property if no existing property is found.</param>
        /// <returns></returns>
        public T GetTrackedProperty<T>(string propertyPath, bool create = true) where T : ITrackedProperty, new()
        {
            var prop = GetTrackedProperty(propertyPath);
            if (prop is T tVal)
                return tVal;
            return create ? AddTrackedProperty<T>(propertyPath) : default;
        }

        /// <summary>
        /// Return the tracked property for the property path.
        /// </summary>
        /// <param name="propertyPath">The serialized property path.</param>
        /// <returns></returns>
        public virtual ITrackedProperty GetTrackedProperty(string propertyPath) => m_PropertiesLookup.TryGetValue(propertyPath, out var property) ? property : null;

        public virtual ITrackedProperty CreateCustomTrackedProperty(string propertyPath) => null;

        /// <summary>
        /// Apply the <see cref="TrackedProperties"/> for <paramref name="variantLocale"/>.
        /// If a value does not exist for this locale then the value for <paramref name="defaultLocale"/> is used as a fallback.
        /// </summary>
        /// <param name="variantLocale">The chosen variant to apply to <see cref="Target"/>.</param>
        /// <param name="defaultLocale">The fallback <see cref="Locale"/> to use when a value does not exist for this variant.</param>
        public abstract AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale);

        /// <summary>
        /// Called when the variants have been applied to <see cref="Target"/>.
        /// </summary>
        protected virtual void PostApplyTrackedProperties() {}

        public void OnAfterDeserialize()
        {
            m_PropertiesLookup.Clear();
            foreach (var trackedProperty in m_TrackedProperties.items)
            {
                if (trackedProperty != null)
                    m_PropertiesLookup[trackedProperty.PropertyPath] = trackedProperty;
            }
        }

        public void OnBeforeSerialize()
        {
            m_TrackedProperties.items.Clear();
            foreach (var trackedProperty in m_PropertiesLookup.Values)
            {
                m_TrackedProperties.items.Add(trackedProperty);
            }
        }
    }
}

#endif
