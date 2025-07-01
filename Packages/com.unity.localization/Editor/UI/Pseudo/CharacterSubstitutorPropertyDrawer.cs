using UnityEngine;
using UnityEngine.Localization.Pseudo;

namespace UnityEditor.Localization.UI
{
    class CharacterSubstitutorPropertyDrawerData : PropertyDrawerExtendedData
    {
        public SerializedProperty substitutionMethod;
        public SerializedProperty replacementsMap;
        public SerializedProperty replacementList;
        public SerializedProperty listMode;
        public bool m_IsAccenter;
    }

    [CustomPropertyDrawer(typeof(CharacterSubstitutor))]
    class CharacterSubstitutorPropertyDrawer : PropertyDrawerExtended<CharacterSubstitutorPropertyDrawerData>
    {
        public override CharacterSubstitutorPropertyDrawerData CreatePropertyData(SerializedProperty property)
        {
            var data = new CharacterSubstitutorPropertyDrawerData
            {
                substitutionMethod = property.FindPropertyRelative("m_SubstitutionMethod"),
                replacementsMap = property.FindPropertyRelative("m_ReplacementsMap"),
                replacementList = property.FindPropertyRelative("m_ReplacementList"),
                listMode = property.FindPropertyRelative("m_ListMode")
            };

            var type = ManagedReferenceUtility.GetType(property.managedReferenceFullTypename);
            data.m_IsAccenter = type == typeof(Accenter);
            return data;
        }

        const float k_RemoveButtonSize = 20;

        static char[] s_DefferedAddTypicalCharacters;

        class Styles
        {
            public static readonly GUIContent addItem = new GUIContent("+");
            public static readonly GUIContent addTypicalCharacterSet = EditorGUIUtility.TrTextContent("Add Typical Character Set");
            public static readonly GUIContent original = EditorGUIUtility.TrTextContent("Original");
            public static readonly GUIContent removeItem = new GUIContent("-");
            public static readonly GUIContent replacement = EditorGUIUtility.TrTextContent("Replacement");
            public static readonly GUIContent replacementCharacters = EditorGUIUtility.TrTextContent("Replacement Characters");
        }

        public override void OnGUI(CharacterSubstitutorPropertyDrawerData data, Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            position.MoveToNextLine();

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                if (!data.m_IsAccenter)
                {
                    EditorGUI.PropertyField(position, data.substitutionMethod);
                    position.MoveToNextLine();
                }

                var method = (CharacterSubstitutor.SubstitutionMethod)data.substitutionMethod.intValue;
                if (method == CharacterSubstitutor.SubstitutionMethod.Map)
                {
                    DrawReplacementRules(position, data.replacementsMap);
                }
                else if (method == CharacterSubstitutor.SubstitutionMethod.List)
                {
                    EditorGUI.BeginDisabledGroup(data.replacementList.arraySize <= 1);
                    EditorGUI.PropertyField(position, data.listMode);
                    EditorGUI.EndDisabledGroup();
                    position.MoveToNextLine();

                    // Add typical characters button
                    position.height = EditorGUI.GetPropertyHeight(data.replacementList, true);
                    EditorGUI.PropertyField(position, data.replacementList, true);
                    position.MoveToNextLine();

                    if (s_DefferedAddTypicalCharacters != null)
                    {
                        foreach (var c in s_DefferedAddTypicalCharacters)
                        {
                            var element = data.replacementList.AddArrayElement();
                            element.intValue = c;
                        }

                        s_DefferedAddTypicalCharacters = null;
                    }

                    position.height = EditorGUIUtility.singleLineHeight;
                    position.xMin += EditorGUIUtility.labelWidth;
                    if (EditorGUI.DropdownButton(position, Styles.addTypicalCharacterSet, FocusType.Keyboard))
                    {
                        var menu = new GenericMenu();

                        foreach (var lang in TypicalCharacterSets.s_TypicalCharacterSets)
                        {
                            menu.AddItem(new GUIContent(lang.Key.ToString()), false, (obj) =>
                            {
                                // We defer the add operation as we are not inside of the SerializedProperty Update/ApplyModifiedChanges.
                                s_DefferedAddTypicalCharacters = lang.Value;
                            }, null);
                        }
                        menu.DropDown(position);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        internal static Rect DrawReplacementRules(Rect position, SerializedProperty property)
        {
            // Header
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, Styles.replacementCharacters, true);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
                return position;

            float indent = EditorGUI.indentLevel * 15;
            float width = position.width - indent;
            var originalPos = new Rect(position.x + indent, position.y, width * 0.5f, position.height);
            var replacementPos = new Rect(originalPos.xMax + 2, position.y, width - originalPos.width - 2 - k_RemoveButtonSize, position.height);
            var btnPos = new Rect(replacementPos.xMax + 2, position.y, k_RemoveButtonSize - 2, position.height);
            GUI.Label(originalPos, Styles.original);
            GUI.Label(replacementPos, Styles.replacement);

            if (GUI.Button(btnPos, Styles.addItem))
            {
                var element = property.AddArrayElement();
                var original = element.FindPropertyRelative("original");
                var replacement = element.FindPropertyRelative("replacement");
                original.intValue = ('A' + property.arraySize - 1);
                replacement.intValue = 0;
            }

            for (int i = 0; i < property.arraySize; ++i)
            {
                originalPos.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                replacementPos.y = btnPos.y = originalPos.y;

                var element = property.GetArrayElementAtIndex(i);
                var original = element.FindPropertyRelative("original");
                var replacement = element.FindPropertyRelative("replacement");

                EditorGUI.PropertyField(originalPos, original, GUIContent.none);
                EditorGUI.PropertyField(replacementPos, replacement, GUIContent.none);

                if (GUI.Button(btnPos, Styles.removeItem))
                {
                    property.DeleteArrayElementAtIndex(i);
                }

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            position.y = originalPos.y;
            return position;
        }

        public override float GetPropertyHeight(CharacterSubstitutorPropertyDrawerData data, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                if (!data.m_IsAccenter)
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var method = (CharacterSubstitutor.SubstitutionMethod)data.substitutionMethod.intValue;

                if (method == CharacterSubstitutor.SubstitutionMethod.Map)
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    if (data.replacementsMap.isExpanded)
                    {
                        height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (1 + data.replacementsMap.arraySize);
                    }
                }
                else if (method == CharacterSubstitutor.SubstitutionMethod.List)
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    height += EditorGUI.GetPropertyHeight(data.replacementList, true);
                    if (data.replacementList.isExpanded)
                    {
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
            return height;
        }
    }
}
