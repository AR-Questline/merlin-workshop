using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization.UI
{
    static class ManagedReferenceUtility
    {
        static readonly Dictionary<object, GUIContent> s_NameLookup = new Dictionary<object, GUIContent>();
        static readonly Dictionary<string, Type> s_TypeLookup = new Dictionary<string, Type>();
        public static readonly GUIContent Empty = new GUIContent("Empty");

        public static Type GetType(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename))
                throw new ArgumentException("String can not be null or empty", nameof(managedReferenceFullTypename));

            if (s_TypeLookup.TryGetValue(managedReferenceFullTypename, out var type))
                return type;

            var typeNames = managedReferenceFullTypename.Split(' ');
            if (typeNames?.Length == 2)
                type = Type.GetType($"{typeNames[1]}, {typeNames[0]}");
            s_TypeLookup[managedReferenceFullTypename] = type;
            return type;
        }

        public static GUIContent GetDisplayName(Type type)
        {
            if (s_NameLookup.TryGetValue(type, out var name))
                return name;

            var itemAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
            if (itemAttribute != null && !string.IsNullOrEmpty(itemAttribute.Name))
            {
                Texture icon = null;
                if (!string.IsNullOrEmpty(itemAttribute.IconPath))
                    icon = EditorGUIUtility.FindTexture(itemAttribute.IconPath);

                name = new GUIContent(itemAttribute.Name, icon);
                s_NameLookup[type] = name;
                return name;
            }

            // Class name
            var className = ObjectNames.NicifyVariableName(type.Name);
            var guiContent = new GUIContent(className);
            s_NameLookup[type] = guiContent;
            return guiContent;
        }

        public static GUIContent GetDisplayName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename))
                return Empty;

            if (s_NameLookup.TryGetValue(managedReferenceFullTypename, out var name))
                return name;

            var type = GetType(managedReferenceFullTypename);
            if (type == null)
            {
                Debug.LogWarning($"Could not resolve managed reference type {managedReferenceFullTypename}. A Display name could not be found.");
                name = new GUIContent(managedReferenceFullTypename);
                s_NameLookup[managedReferenceFullTypename] = name;
                return name;
            }

            name = GetDisplayName(type);
            s_NameLookup[managedReferenceFullTypename] = name;
            return name;
        }

        static GUIContent UseClassName(string managedReferenceFullTypename, Type type)
        {
            var name = ObjectNames.NicifyVariableName(type.Name);
            var guiContent = new GUIContent(name);
            s_NameLookup[managedReferenceFullTypename] = guiContent;
            return guiContent;
        }
    }
}
