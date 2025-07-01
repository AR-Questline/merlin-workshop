// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Animancer;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.MoreGUI;
using Awaken.Utility.Enums;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using RichEnumRef = Awaken.TG.Main.Utility.RichEnums.RichEnumReference;

namespace Awaken.TG.Editor.Utility.RichEnumReference {
    /// <summary>
    /// Custom property drawer for <see cref="RichEnumReference"/> properties.
    /// </summary>
    [CustomPropertyDrawer(typeof(RichEnumRef))]
    [CustomPropertyDrawer(typeof(RichEnumExtendsAttribute), true)]
    public sealed class RichEnumReferencePropertyDrawer : PropertyDrawer {
        // === Type Filtering

        /// <summary>
        /// Gets or sets a function that returns a collection of types that are
        /// to be excluded from drop-down. A value of <c>null</c> specifies that
        /// no types are to be excluded.
        /// </summary>
        static Func<ICollection<Type>> ExcludedTypeCollectionGetter { get; set; }

        static readonly Dictionary<string, string> Searches = new();
        static readonly List<Type> ReusableTypes = new();

        static void GetFilteredEnums(RichEnumExtendsAttribute filter, string search, List<RichEnum> enums) {
            var excludedTypes = ExcludedTypeCollectionGetter?.Invoke();

            var richEnumsTypes = TypeCache.GetTypesDerivedFrom<RichEnum>();
            ReusableTypes.Clear();
            FilterTypes(richEnumsTypes, filter, excludedTypes, ReusableTypes);

            foreach (var type in ReusableTypes) {
                if (!type.ContainsGenericParameters) {
                    if (string.IsNullOrWhiteSpace(search)) {
                        enums.AddRange(GetEnums(type));
                    } else {
                        var enumsOfType = GetEnums(type).Where(e =>
                            FormatGroupedTypeName(e, filter).IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1);
                        enums.AddRange(enumsOfType);
                    }
                }
            }

            ReusableTypes.Clear();
            enums.Sort();
        }

        static List<RichEnum> GetEnums(Type type) {
            List<RichEnum> enums = new();
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo field in fields) {
                if (field.IsStatic && field.GetValue(null) is RichEnum richEnum) {
                    enums.Add(richEnum);
                }
            }

            return enums;
        }

        static void FilterTypes(TypeCache.TypeCollection types, RichEnumExtendsAttribute filter, ICollection<Type> excludedTypes, List<Type> output) {
            foreach (var type in types) {
                if (filter != null && !filter.ShowOthers && !filter.IsConstraintSatisfied(type)) {
                    continue;
                }

                if (excludedTypes != null && excludedTypes.Contains(type)) {
                    continue;
                }

                output.Add(type);
            }
        }

        // === Control Drawing / Event Handling
        const string MultiEditing = "(Mixed Values)";
        const string None = "(None)";

        public static string DisplayNameFromRef(string enumRef) {
            if (enumRef.IsNullOrWhitespace()) {
                return None;
            }

            return enumRef[(enumRef.LastIndexOf(':') + 1)..];
        }

        public static void DrawSelectionControl(Rect position, GUIContent label, string selectedEnumRef, ref string search, RichEnumExtendsAttribute filter,
            Action<RichEnum> onEnumRefChanged) {
            try {
                var enumRect = new Rect(position.x, position.y, position.width, position.height);

                List<string> possiblePaths = new(){None};
                List<GUIContent> possiblePathsAsContent = new(){new GUIContent(None)};
                List<GUIContent> namesAsContent = new(){new GUIContent(None)};

                List<RichEnum> filteredTypes = new();
                List<RichEnum> possibleTypes = new();
                GetFilteredEnums(filter, search, filteredTypes);

                bool filterInspectorCategories = filter != null && !filter.InspectorCategories.IsNullOrEmpty();
                bool alwaysDisplayCategory = filteredTypes.Any(e => AttributesCache.GetCustomAttribute<RichEnumAlwaysDisplayCategoryAttribute>(e.GetType()) != null);
                bool showTypeName = alwaysDisplayCategory || (filteredTypes.Count > 0 && filteredTypes.Any(e => e.GetType() != filteredTypes[0].GetType()));
                if (showTypeName) {
                    DictionaryPool<RichEnum, string>.Get(out var formattedNames);
                    foreach (var t in filteredTypes) {
                        formattedNames[t] = FormatGroupedTypeName(t, filter);
                    }
                    filteredTypes.Sort((e1, e2) => string.Compare(formattedNames[e1], formattedNames[e2], StringComparison.Ordinal));
                    formattedNames.Clear();
                    DictionaryPool<RichEnum, string>.Release(formattedNames);
                }

                foreach (RichEnum t in filteredTypes) {
                    string menuLabel = showTypeName ? FormatGroupedTypeName(t, filter) : t.EnumName;
                    bool itemShouldBeSkipped = string.IsNullOrEmpty(menuLabel) || menuLabel.ToLower().Contains("hide") ||
                                               (filterInspectorCategories && !filter.InspectorCategories.Contains(t.InspectorCategory));
                    if (itemShouldBeSkipped)
                        continue;

                    possibleTypes.Add(t);
                    possiblePathsAsContent.Add(new GUIContent(menuLabel));
                    possiblePaths.Add(RichEnumRef.GetEnumRef(t));
                    namesAsContent.Add(new GUIContent(t.EnumName));
                }

                string current = string.IsNullOrEmpty(selectedEnumRef) ? None : selectedEnumRef;
                int index = possiblePaths.IndexOf(current);
                if (index == -1) {
                    Log.Minor?.Error($"Invalid enum reference: {selectedEnumRef}, setting NONE instead. (Did you delete the enum?)");
                    onEnumRefChanged.Invoke(null);
                    return;
                }
                
                EditorGUI.BeginChangeCheck();
                index = AREditorPopup.Draw(enumRect, label, index, possiblePathsAsContent.ToArray(), namesAsContent.ToArray());
                if (EditorGUI.EndChangeCheck()) {
                    if (index == 0) {
                        onEnumRefChanged.Invoke(null);
                    } else {
                        var enumValue = possibleTypes[index - 1];
                        onEnumRefChanged.Invoke(enumValue);
                    }
                }
            } finally {
                ExcludedTypeCollectionGetter = null;
            }
        }

        static void DrawSelectionControl(Rect position, SerializedProperty property, GUIContent label, RichEnumExtendsAttribute filter, ref string search) {
            DrawSelectionControl(position, label, property.stringValue, ref search, filter, richEnum => OnSelectedEnumName(richEnum, property));
        }

        // === Helpers
        static string FormatGroupedTypeName(RichEnum richEnum, RichEnumExtendsAttribute filter) {
            if (filter == null) {
                Log.Minor?.Error("RichEnumReference without RichEnumExtendsAttribute!");
                return richEnum.EnumName;
            }
            Type enumType = richEnum.GetType();
            return filter.OnlyEnumNames
                ? richEnum.EnumName
                : Name(richEnum, filter, AttributesCache.GetCustomAttribute<RichEnumDisplayCategoryAttribute>(enumType),
                    AttributesCache.GetCustomAttribute<RichEnumAlwaysDisplayCategoryAttribute>(enumType) != null);
        }

        static string Name(RichEnum richEnum, RichEnumExtendsAttribute filter, RichEnumDisplayCategoryAttribute enumCategory, bool alwaysDisplayCategory) {
            Type type = richEnum.GetType();
            string typeName = richEnum.GetType().ToString();
            string displayName = typeName.Substring(typeName.LastIndexOf('.') + 1).Replace('+', '/');

            string category = enumCategory != null ? enumCategory.Category : displayName;
            string baseString;
            if (string.IsNullOrWhiteSpace(category) && alwaysDisplayCategory) {
                baseString = string.IsNullOrWhiteSpace(richEnum.InspectorCategory)
                    ? $"{richEnum.EnumName}"
                    : $"{richEnum.InspectorCategory}/{richEnum.EnumName}";
            } else {
                baseString = string.IsNullOrWhiteSpace(richEnum.InspectorCategory)
                    ? $"{category}/{richEnum.EnumName}"
                    : $"{category}/{richEnum.InspectorCategory}/{richEnum.EnumName}";
            }

            if (filter != null && !filter.IsConstraintSatisfied(type)) {
                return $"Others/{baseString}";
            }

            return baseString;
        }

        static void OnSelectedEnumName(RichEnum userData, SerializedProperty property) {
            property.stringValue = RichEnumRef.GetEnumRef(userData);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            bool hasSearchBox = fieldInfo?.GetCustomAttribute<RichEnumSearchBoxAttribute>() != null;
            return EditorStyles.popup.CalcHeight(GUIContent.none, 0) + (hasSearchBox ? EditorGUIUtility.singleLineHeight : 0);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            string firstKey = targetObject.GetInstanceID() + property.name + property.propertyPath;
            if (!Searches.TryGetValue(firstKey, out var search)) {
                Searches.Add(firstKey, "");
                search = "";
            }

            using var mixedValues = new EditorGUI.MixedValueScope(property.hasMultipleDifferentValues);
            DrawSelectionControl(position, property.FindPropertyRelative("_enumRef"), label, attribute as RichEnumExtendsAttribute, ref search);

            if (property.hasMultipleDifferentValues || search == MultiEditing) return;

            for (var index = 0; index < property.serializedObject.targetObjects.Length; index++) {
                string key = targetObject.GetInstanceID() + property.name + property.propertyPath;
                Searches[key] = search;
            }
        }
    }
}