using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace UnityEngine.Localization
{
    #if UNITY_EDITOR
    /// <summary>
    /// Driver for properties that are modified by event calls.
    /// </summary>
    internal class LocalizationPropertyDriver : PropertyDriver<LocalizationPropertyDriver>
    {
        /// <summary>
        /// When making a change using a UnityEvent we need to determine what serialized property this change will affect In order to register the property as driven.
        /// This maps known UnityEvent target types and method names to their serialized property paths so that they can be marked as driven.
        /// </summary>
        internal static Dictionary<(Type targetType, string methodName), string> UnityEventDrivenPropertiesLookup { get; } = new Dictionary<(Type targetType, string methodName), string>();
    }
    #endif

    /// <summary>
    /// Allows for making temporary changes to Components in a scene whilst previewing a Locale.
    /// Any changes made to a property that is marked as driven will be ignored when saving the scene
    /// and reverted when the property is unregistered or <see cref="LocalizationSettings.SelectedLocale"/> is set to <see langword="null"/>.
    /// </summary>
    public static class EditorPropertyDriver
    {
        /// <summary>
        /// Mark the property as Driven in the editor.
        /// When a property is marked as driven it is considered to be a temporary change, that is the new values applied to the property
        /// will be ignored and not saved into the scene. The value will revert back to its original value when <see cref="UnregisterProperty(Object, string)"/>
        /// is called or <see cref="LocalizationSettings.SelectedLocale"/> is set to <see langword="null"/>.
        /// Calling this method in play mode or a player build will do nothing.
        /// </summary>
        /// <example>
        /// This shows how to support non-destructive Edit Mode changes using <see cref="EditorPropertyDriver"/>.
        /// <code source="../../DocCodeSamples.Tests/EditorPropertyDriverSamples.cs"/>
        /// </example>
        /// <param name="target">The object that the property is part of.</param>
        /// <param name="propertyPath">The serialized property path. The value that would be used to access using a [SerializedProperty](https://docs.unity3d.com/ScriptReference/SerializedProperty.html)</param>
        public static void RegisterProperty(Object target, string propertyPath)
        {
            #if UNITY_EDITOR
            if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                LocalizationPropertyDriver.RegisterProperty(target, propertyPath);
            #endif
        }

        /// <summary>
        /// Removed the property tracking and reverts the value back to the original value it was before <see cref="RegisterProperty(Object, string)"/> was called.
        /// In most cases you will not need to call this unless the driven properties are likely to change dynamically.
        /// Calling this method in play mode or a player build will do nothing.
        /// </summary>
        /// <param name="target">The object that the property is part of.</param>
        /// <param name="propertyPath">The serialized property path. The value that would be used to access using a [SerializedProperty](https://docs.unity3d.com/ScriptReference/SerializedProperty.html)</param>
        public static void UnregisterProperty(Object target, string propertyPath)
        {
            #if UNITY_EDITOR
            if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                LocalizationPropertyDriver.RegisterProperty(target, propertyPath);
            #endif
        }
    }
}
