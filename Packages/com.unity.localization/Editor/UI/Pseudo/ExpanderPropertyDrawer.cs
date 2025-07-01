using UnityEngine;
using UnityEngine.Localization.Pseudo;

namespace UnityEditor.Localization.UI
{
    class ExpanderPropertyDrawerData : PropertyDrawerExtendedData
    {
        public SerializedProperty expansionRules;
        public SerializedProperty location;
        public SerializedProperty minimumStringLength;
        public SerializedProperty paddingCharacters;
    }

    [CustomPropertyDrawer(typeof(Expander))]
    class ExpanderPropertyDrawer : PropertyDrawerExtended<ExpanderPropertyDrawerData>
    {
        const float k_DefaultExpansion = 0.3f;
        const float k_RemoveButtonSize = 20;

        class Styles
        {
            public static readonly GUIContent addItem = new GUIContent("+");
            public static readonly GUIContent expansionAmount = EditorGUIUtility.TrTextContent("Expansion", "The amount to increase the string size. For example 0.3 would add 30% onto the length.");
            public static readonly GUIContent removeItem = new GUIContent("-");
            public static readonly GUIContent stringLength = EditorGUIUtility.TrTextContent("String Length", "The length the string should fall within for this rule to be applied.");
        }

        static (SerializedProperty min, SerializedProperty max, SerializedProperty rate) ExtractExpansionRuleProperties(SerializedProperty prop)
        {
            var min = prop.FindPropertyRelative("m_MinCharacters");
            var max = prop.FindPropertyRelative("m_MaxCharacters");
            var rate = prop.FindPropertyRelative("m_ExpansionAmount");
            return (min, max, rate);
        }

        public override ExpanderPropertyDrawerData CreatePropertyData(SerializedProperty property)
        {
            return new ExpanderPropertyDrawerData
            {
                expansionRules = property.FindPropertyRelative("m_ExpansionRules"),
                location = property.FindPropertyRelative("m_Location"),
                minimumStringLength = property.FindPropertyRelative("m_MinimumStringLength"),
                paddingCharacters = property.FindPropertyRelative("m_PaddingCharacters")
            };
        }

        public override void OnGUI(ExpanderPropertyDrawerData data, Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position = DrawExpansionRules(position, data);

                EditorGUI.PropertyField(position, data.location);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(position, data.minimumStringLength);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                position.height = EditorGUI.GetPropertyHeight(data.paddingCharacters, true);
                EditorGUI.PropertyField(position, data.paddingCharacters, true);
                EditorGUI.indentLevel--;
            }
        }

        Rect DrawExpansionRules(Rect position, ExpanderPropertyDrawerData data)
        {
            // Header
            float indent = EditorGUI.indentLevel * 15;
            float width = position.width - indent;
            var rangePos = new Rect(position.x + indent, position.y, width * 0.5f, position.height);
            var valuePos = new Rect(rangePos.xMax + 2, position.y, width - rangePos.width - 2 - k_RemoveButtonSize, position.height);
            var addBtnPos = new Rect(valuePos.xMax + 2, position.y, k_RemoveButtonSize - 2, position.height);
            GUI.Label(rangePos, Styles.stringLength);
            GUI.Label(valuePos, Styles.expansionAmount);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Always have at least 1 item
            if (data.expansionRules.arraySize == 0)
            {
                // Add defaults
                data.expansionRules.InsertArrayElementAtIndex(0);
                var properties = ExtractExpansionRuleProperties(data.expansionRules.GetArrayElementAtIndex(0));
                properties.min.intValue = 0;
                properties.max.intValue = int.MaxValue;
                properties.rate.floatValue = k_DefaultExpansion;
            }

            // Add button
            if (GUI.Button(addBtnPos, Styles.addItem))
            {
                var addedItem = ExtractExpansionRuleProperties(data.expansionRules.AddArrayElement());
                addedItem.max.intValue = int.MaxValue;
            }

            for (int i = 0; i < data.expansionRules.arraySize; ++i)
            {
                DrawExpansionRuleItem(position, i, data);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return position;
        }

        void DrawExpansionRuleItem(Rect position, int index, ExpanderPropertyDrawerData data)
        {
            var properties = ExtractExpansionRuleProperties(data.expansionRules.GetArrayElementAtIndex(index));

            EditorGUI.BeginChangeCheck();

            var rangePos = new Rect(position.x, position.y, position.width * 0.5f, position.height);
            var valuePos = new Rect(rangePos.xMax + 2, position.y, position.width - rangePos.width - 2 - k_RemoveButtonSize, position.height);
            var removeBtnPos = new Rect(valuePos.xMax + 2, position.y, k_RemoveButtonSize - 2, position.height);

            int prevMaxValue = 0;
            if (index > 0)
            {
                var previous = ExtractExpansionRuleProperties(data.expansionRules.GetArrayElementAtIndex(index - 1));
                prevMaxValue = previous.max.intValue;

                if (prevMaxValue > properties.min.intValue)
                    properties.min.intValue = prevMaxValue;
            }

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;
            var newMax = EditorGUI.IntField(rangePos, $"{prevMaxValue} - ", properties.max.intValue);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            var newExpansion = EditorGUI.FloatField(valuePos, properties.rate.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                properties.max.intValue = newMax;
                properties.rate.floatValue = newExpansion;
            }

            if (GUI.Button(removeBtnPos, Styles.removeItem))
            {
                data.expansionRules.DeleteArrayElementAtIndex(index);
            }
        }

        public override float GetPropertyHeight(ExpanderPropertyDrawerData data, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                height += EditorGUI.GetPropertyHeight(data.paddingCharacters, true) + EditorGUIUtility.standardVerticalSpacing;
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing); // Rule header and + button
                height += data.expansionRules.arraySize * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }
            return height;
        }
    }
}
