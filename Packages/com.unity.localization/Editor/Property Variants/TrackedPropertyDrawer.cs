#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEngine;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;

namespace UnityEditor.Localization.PropertyVariants
{
    [CustomPropertyDrawer(typeof(ArraySizeTrackedProperty))]
    [CustomPropertyDrawer(typeof(BoolTrackedProperty))]
    [CustomPropertyDrawer(typeof(ByteTrackedProperty))]
    [CustomPropertyDrawer(typeof(CharTrackedProperty))]
    [CustomPropertyDrawer(typeof(DoubleTrackedProperty))]
    [CustomPropertyDrawer(typeof(FloatTrackedProperty))]
    [CustomPropertyDrawer(typeof(IntTrackedProperty))]
    [CustomPropertyDrawer(typeof(LongTrackedProperty))]
    [CustomPropertyDrawer(typeof(SByteTrackedProperty))]
    [CustomPropertyDrawer(typeof(ShortTrackedProperty))]
    [CustomPropertyDrawer(typeof(StringTrackedProperty))]
    [CustomPropertyDrawer(typeof(UIntTrackedProperty))]
    [CustomPropertyDrawer(typeof(ULongTrackedProperty))]
    [CustomPropertyDrawer(typeof(UnityObjectProperty), true)]
    [CustomPropertyDrawer(typeof(UShortTrackedProperty))]
    class TrackedPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent s_AddLabel = EditorGUIUtility.TrTextContent("Add Variant");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var path = property.FindPropertyRelative("m_PropertyPath");
            var variants = property.FindPropertyRelative("m_VariantData");

            position.height = EditorGUIUtility.singleLineHeight;
            variants.isExpanded = EditorGUI.Foldout(position, variants.isExpanded, ObjectNames.NicifyVariableName(path.stringValue), true);
            position.MoveToNextLine();

            if (variants.isExpanded)
            {
                EditorGUI.indentLevel++;

                var variantsLookup = DictionaryPool<string, (int index, SerializedProperty value)>.Get();
                for (int i = 0; i < variants.arraySize; ++i)
                {
                    var item = variants.GetArrayElementAtIndex(i);
                    var code = item.FindPropertyRelative("localeIdentifier.m_Code");
                    variantsLookup[code.stringValue] = (i, item.FindPropertyRelative("value"));
                }

                foreach (var locale in LocalizationEditorSettings.GetLocales())
                {
                    var split = position.SplitHorizontalFixedWidthRight(17);

                    if (variantsLookup.TryGetValue(locale.Identifier.Code, out var item))
                    {
                        EditorGUI.BeginChangeCheck();
                        DrawValueField(split.left, item.value, new GUIContent(locale.LocaleName), path.stringValue);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (LocalizationSettings.SelectedLocale == null || LocalizationSettings.SelectedLocale != locale)
                                break;

                            if (property.serializedObject.targetObject is GameObjectLocalizer localizer)
                                localizer.ApplyLocaleVariant(locale);
                        }

                        if (GUI.Button(split.right, GUIContent.none, "OL Minus"))
                        {
                            variants.DeleteArrayElementAtIndex(item.index);
                            break; // Break as our list data is no longer valid now and will produce errors if we try and use it.
                        }
                    }
                    else
                    {
                        var buttonPos = EditorGUI.PrefixLabel(position, new GUIContent(locale.LocaleName));
                        if (GUI.Button(buttonPos, s_AddLabel))
                        {
                            var element = variants.AddArrayElement();
                            var code = element.FindPropertyRelative("localeIdentifier.m_Code");
                            code.stringValue = locale.Identifier.Code;
                        }
                    }

                    position.MoveToNextLine();
                }
                EditorGUI.indentLevel--;
                DictionaryPool<string, (int index, SerializedProperty value)>.Release(variantsLookup);
            }
        }

        protected virtual void DrawValueField(Rect position, SerializedProperty value, GUIContent label, string propertyPath)
        {
            EditorGUI.PropertyField(position, value, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var variants = property.FindPropertyRelative("m_VariantData");
            if (variants.isExpanded)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * LocalizationEditorSettings.GetLocales().Count;
            return height;
        }
    }
}

#endif
