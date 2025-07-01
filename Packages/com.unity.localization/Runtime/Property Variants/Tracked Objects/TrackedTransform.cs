#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.PropertyVariants.TrackedObjects
{
    /// <summary>
    /// Tracks and applies variant changes to a [Transform](https://docs.unity3d.com/ScriptReference/Transform.html).
    /// </summary>
    [Serializable]
    [DisplayName("Transform")]
    [CustomTrackedObject(typeof(Transform), false)]
    public class TrackedTransform : TrackedObject
    {
        Vector3 m_PositionToApply;
        Quaternion m_RotationToApply;
        Vector3 m_ScaleToApply;

        Dictionary<string, Action<float>> m_PropertyHandlers;

        protected virtual void AddPropertyHandlers(Dictionary<string, Action<float>> handlers)
        {
            handlers["m_LocalPosition.x"] = val => m_PositionToApply.x = val;
            handlers["m_LocalPosition.y"] = val => m_PositionToApply.y = val;
            handlers["m_LocalPosition.z"] = val => m_PositionToApply.z = val;

            handlers["m_LocalRotation.x"] = val => m_RotationToApply.x = val;
            handlers["m_LocalRotation.y"] = val => m_RotationToApply.y = val;
            handlers["m_LocalRotation.z"] = val => m_RotationToApply.z = val;
            handlers["m_LocalRotation.w"] = val => m_RotationToApply.w = val;

            handlers["m_LocalScale.x"] = val => m_ScaleToApply.x = val;
            handlers["m_LocalScale.y"] = val => m_ScaleToApply.y = val;
            handlers["m_LocalScale.z"] = val => m_ScaleToApply.z = val;
        }

        // Note: m_RootOrder has been removed in 2023 and may be backported in the future. (LOC-917)
        public override bool CanTrackProperty(string propertyPath) => !(propertyPath.StartsWith("m_LocalEulerAnglesHint") || propertyPath == "m_RootOrder");

        public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
        {
            var transform = (Transform)Target;

            if (m_PropertyHandlers == null)
            {
                m_PropertyHandlers = new Dictionary<string, Action<float>>();
                AddPropertyHandlers(m_PropertyHandlers);
            }

            // Grab the current values
            m_PositionToApply = transform.localPosition;
            m_RotationToApply = transform.localRotation;
            m_ScaleToApply = transform.localScale;

            // Iterate through the tracked properties and use the property handlers to apply changes.

            var variantIdentifier = variantLocale.Identifier;
            var fallbackIdentifier = defaultLocale != null ? defaultLocale.Identifier : default;

            foreach (var property in TrackedProperties)
            {
                #if UNITY_EDITOR
                VariantsPropertyDriver.RegisterProperty(Target, property.PropertyPath);
                #endif

                var floatProperty = (FloatTrackedProperty)property;
                if (floatProperty.GetValue(variantIdentifier, fallbackIdentifier, out var val) &&
                    m_PropertyHandlers.TryGetValue(property.PropertyPath, out var handler))
                {
                    handler(val);
                }
            }

            // Apply the changes
            transform.localScale = m_ScaleToApply;
            transform.localPosition = m_PositionToApply;
            transform.localRotation = m_RotationToApply;

            return default;
        }
    }
}

#endif
