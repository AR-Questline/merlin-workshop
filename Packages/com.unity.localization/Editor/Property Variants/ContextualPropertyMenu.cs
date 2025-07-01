#if ENABLE_PROPERTY_VARIANTS

using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;

namespace UnityEditor.Localization.PropertyVariants
{
    [InitializeOnLoad]
    static class ContextualPropertyMenu
    {
        internal static readonly string k_RemoveVariantLabel = L10n.Tr("Remove Localized Property Variant ({0})");
        internal static readonly GUIContent k_AddLabel = EditorGUIUtility.TrTextContent("Localize Property");
        internal static readonly GUIContent k_RemoveLabel = EditorGUIUtility.TrTextContent("Remove Localized Property");

        static ContextualPropertyMenu() => EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;

        public enum PropertyState
        {
            Tracked,
            TrackedWithOverride,
            CanBeTracked,
            CanNotBeTracked
        }

        public static void RemoveSeperator(GenericMenu menu)
        {
            // contextualPropertyMenu assumes we will always add menu items so it adds a seperator.
            // If we dont add anything we should remove this so the menu does not look strange.
            var field = typeof(GenericMenu).GetField("m_MenuItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.GetValue(menu) is IList menuItems && menuItems.Count > 0)
            {
                var menuItem = menuItems[menuItems.Count - 1];
                var separator = menuItem.GetType().GetField("separator");
                if (separator != null && (bool)separator.GetValue(menuItem))
                {
                    menuItems.RemoveAt(menuItems.Count - 1);
                }
            }
        }

        public static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            // Ignore anything to do with GameObjectLocalizer.
            if (property.serializedObject.targetObject is GameObjectLocalizer)
            {
                RemoveSeperator(menu);
                return;
            }

            // Make a copy so that it does not change when being iterated by a property drawer etc.
            property = property.Copy();

            var state = GetPropertyState(property);
            if (state == PropertyState.Tracked || state == PropertyState.TrackedWithOverride)
            {
                menu.AddItem(k_RemoveLabel, false, () => RemoveTrackedProperty(property));

                if (state == PropertyState.TrackedWithOverride)
                {
                    var label = new GUIContent(string.Format(k_RemoveVariantLabel, LocalizationSettings.SelectedLocale));
                    menu.AddItem(label, false, () => RemoveTrackedPropertyOverride(property, LocalizationSettings.SelectedLocale.Identifier));
                }
            }
            else if (state == PropertyState.CanBeTracked)
            {
                menu.AddItem(k_AddLabel, false, () => AddProperty(property));
            }
            else
            {
                RemoveSeperator(menu);
            }
        }

        public static PropertyState GetPropertyState(SerializedProperty property)
        {
            PropertyState propertyState = GetPropertyState(property.serializedObject.targetObjects[0] as Component, property.propertyPath);

            if (property.serializedObject.targetObjects.Length > 1)
            {
                for (int i = 1; i < property.serializedObject.targetObjects.Length; i++)
                {
                    // All states must be the same when using multi select
                    if (GetPropertyState(property.serializedObject.targetObjects[i] as Component, property.propertyPath) != propertyState)
                        return PropertyState.CanNotBeTracked;
                }
            }

            return propertyState;
        }

        static PropertyState GetPropertyState(Component targetComponent, string propertyPath)
        {
            if (targetComponent == null)
                return PropertyState.CanNotBeTracked;

            var localizer = targetComponent.GetComponent<GameObjectLocalizer>();
            var trackedObject = localizer?.GetTrackedObject(targetComponent) ?? TrackedObjectFactory.CreateTrackedObject(targetComponent);
            if (trackedObject == null)
                return PropertyState.CanNotBeTracked;

            if (trackedObject.GetTrackedProperty(propertyPath) is ITrackedProperty trackedProperty)
            {
                if (LocalizationSettings.SelectedLocale != null &&
                    LocalizationSettings.SelectedLocale != LocalizationSettings.ProjectLocale &&
                    trackedProperty.HasVariant(LocalizationSettings.SelectedLocale.Identifier))
                {
                    return PropertyState.TrackedWithOverride;
                }
                return PropertyState.Tracked;
            }

            if (trackedObject.CanTrackProperty(propertyPath) && (trackedObject.CreateCustomTrackedProperty(propertyPath) ?? TrackedObjectFactory.CreateTrackedProperty(targetComponent, propertyPath, false)) is ITrackedProperty)
                return PropertyState.CanBeTracked;

            return PropertyState.CanNotBeTracked;
        }

        public static void RemoveTrackedProperty(SerializedProperty property)
        {
            using (new UndoScope("Remove localized property", true))
            {
                foreach (Component targetComponent in property.serializedObject.targetObjects)
                {
                    if (targetComponent == null || !(targetComponent.GetComponent<GameObjectLocalizer>() is GameObjectLocalizer localizer))
                        continue;

                    var trackedObject = localizer.GetTrackedObject(targetComponent);
                    if (trackedObject?.GetTrackedProperty(property.propertyPath) is ITrackedProperty trackedProperty)
                    {
                        Undo.RecordObject(localizer, "Remove property variant");
                        trackedObject.RemoveTrackedProperty(trackedProperty);

                        if (trackedObject.TrackedProperties.Count == 0)
                            localizer.TrackedObjects.Remove(trackedObject);

                        if (localizer.TrackedObjects.Count == 0)
                            Undo.DestroyObjectImmediate(localizer);
                    }
                }
            }

            LocalizationEditorSettings.RefreshEditorPreview();
        }

        public static void RemoveTrackedPropertyOverride(SerializedProperty property, LocaleIdentifier localeIdentifier)
        {
            using (new UndoScope("Remove localized property variant override", true))
            {
                foreach (Component targetComponent in property.serializedObject.targetObjects)
                {
                    if (targetComponent == null || !(targetComponent.GetComponent<GameObjectLocalizer>() is GameObjectLocalizer localizer))
                        continue;

                    var trackedObject = localizer.GetTrackedObject(targetComponent);
                    if (trackedObject?.GetTrackedProperty(property.propertyPath) is ITrackedPropertyRemoveVariant trackedProperty)
                    {
                        Undo.RecordObject(localizer, "Remove localized property variant override");
                        trackedProperty.RemoveVariant(localeIdentifier);
                    }
                }
            }

            LocalizationEditorSettings.RefreshEditorPreview();
        }

        public static void AddProperty(SerializedProperty property)
        {
            if (LocalizationSettings.ProjectLocale == null)
            {
                Debug.LogWarning("Could not configure localized property. Assign a ProjectLocale to the LocalizationSettings.");
                return;
            }

            using (new UndoScope("Add localized property", true))
            {
                foreach (Component targetComponent in property.serializedObject.targetObjects)
                {
                    if (targetComponent == null)
                        continue;

                    var localizer = targetComponent.GetComponent<GameObjectLocalizer>() ?? Undo.AddComponent<GameObjectLocalizer>(targetComponent.gameObject);
                    Undo.RecordObject(localizer, "Add property variant");

                    var trackedObject = localizer.GetTrackedObject(targetComponent);
                    if (trackedObject == null)
                    {
                        trackedObject = TrackedObjectFactory.CreateTrackedObject(targetComponent);
                        localizer.TrackedObjects.Add(trackedObject);
                    }

                    var trackedProperty = trackedObject.CreateCustomTrackedProperty(property.propertyPath) ?? TrackedObjectFactory.CreateTrackedProperty(targetComponent, property.propertyPath, false);
                    trackedObject.AddTrackedProperty(trackedProperty);

                    var componentProperty = property;

                    // We need to create a new SerializedObject to get the value for just this component.
                    if (property.hasMultipleDifferentValues)
                    {
                        var so = new SerializedObject(targetComponent);
                        componentProperty = so.FindProperty(property.propertyPath);
                    }

                    // Prepare the new tracked property
                    if (trackedProperty is IStringProperty stringProperty)
                        stringProperty.SetValueFromString(LocalizationSettings.ProjectLocale.Identifier, componentProperty.GetValueAsString());
                }
            }

            LocalizationEditorSettings.RefreshEditorPreview();
        }
    }
}
#endif
