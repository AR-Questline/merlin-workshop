using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Heroes.Items.LootTables;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Heroes.Items {
    [CustomPropertyDrawer(typeof(ItemSpawningData))]
    public class ItemSpawningDataPropertyDrawer : PropertyDrawer {
        const float ShortLabel = 20;
        const float LongLabel = 50;
        const float MinimalTemplateWidth = 250;
        const float InputFieldMargin = 5;
        
        static readonly float SingleLineHeight = EditorGUIUtility.singleLineHeight;

        DisplayMode _displayMode = DisplayMode.InOneLineLong;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw label
            GUIStyle labelStyle = EditorStyles.label;
            var propertyLabelSize = string.IsNullOrEmpty(label.text) ? Vector2.zero : labelStyle.CalcSize(label) + Vector2.right * 10;
            
            var templateProp = property.FindPropertyRelative("itemTemplateReference");
            var templatePropHeight = templateProp.Height();
            
            Rect propertyLabelRect = new (position.x, position.y, propertyLabelSize.x, propertyLabelSize.y);
            
            _displayMode = GetDisplayMode(position, propertyLabelSize);
            
            bool isOneLine = _displayMode is DisplayMode.InOneLineLong or DisplayMode.InOneLineShort;
            bool displayShort = _displayMode is DisplayMode.InOneLineShort or DisplayMode.InTwoLinesShort;;
            
            float secondLineY = position.y + templatePropHeight + SingleLineHeight + 2;
            float secondLineX = position.x + propertyLabelRect.width;
            
            float paramLabelWidth = GetParamLabelWidth(isOneLine, displayShort, position, propertyLabelSize);
            float templateWidth = GetTemplateWidth(isOneLine, position, propertyLabelSize, paramLabelWidth);
            
            Rect templateRect = GetTemplateRect(position, propertyLabelRect, templateWidth, templatePropHeight);
            Rect quantityLabelRect = GetQuantityLabelRect(isOneLine, position, paramLabelWidth, secondLineX, secondLineY);
            Rect quantityRect = GetQuantityRect(isOneLine, position, paramLabelWidth, quantityLabelRect, secondLineY);
            Rect itemLevelLabelRect = GetItemLevelLabelRect(isOneLine, position, paramLabelWidth, quantityRect, secondLineY);
            Rect itemLevelRect = GetItemLevelRect(isOneLine, position, paramLabelWidth, itemLevelLabelRect, secondLineY);
            
            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            //Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.LabelField(propertyLabelRect, label);
            EditorGUI.PropertyField(templateRect, templateProp, GUIContent.none);
            EditorGUI.LabelField(quantityLabelRect, GetAmountLabel(displayShort));
            EditorGUI.LabelField(itemLevelLabelRect, GetQuantityLabel(displayShort));
            EditorGUI.PropertyField(quantityRect, property.FindPropertyRelative("quantity"), GUIContent.none);
            EditorGUI.PropertyField(itemLevelRect, property.FindPropertyRelative("itemLvl"), GUIContent.none);
            
            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = ReferenceHeight(property); //Template reference height
            return height + EditorGUIUtility.singleLineHeight * 2 + 2;
        }
        
        static DisplayMode GetDisplayMode(Rect position, Vector2 propertyLabelSize) {
            if (position.width >= MinimalTemplateWidth + propertyLabelSize.x + 2 * LongLabel) {
                return DisplayMode.InOneLineLong;
            }

            if (position.width >= MinimalTemplateWidth + propertyLabelSize.x + ShortLabel + LongLabel) {
                return DisplayMode.InOneLineShort;
            }
                
            if (LongLabel * 4 < position.width - propertyLabelSize.x) {
                return DisplayMode.InTwoLinesLong;
            }
            return DisplayMode.InTwoLinesShort;
        }

        static float GetParamLabelWidth(bool isOneLine, bool displayShort, Rect position, Vector2 propertyLabelSize) {
            return isOneLine
                ? displayShort ? ShortLabel * 2 : LongLabel
                : (position.width - propertyLabelSize.x - LongLabel * 2 - 10) / 2;
        }

        static float GetTemplateWidth(bool isOneLine, Rect position, Vector2 propertyLabelSize, float labelWidth) {
            return isOneLine 
                ? position.width - propertyLabelSize.x - labelWidth * 2 
                : position.width - propertyLabelSize.x;
        }

        static Rect GetTemplateRect(Rect position, Rect propertyLabelRect, float templateWidth, float templatePropHeight) {
            return new Rect(position.x + propertyLabelRect.width, position.y, templateWidth, templatePropHeight + SingleLineHeight);
        }

        static Rect GetItemLevelRect(bool isOneLine, Rect position, float labelWidth, Rect itemLevelLabelRect, float secondLineY) {
            return isOneLine 
                ? new Rect(position.x + position.width - labelWidth, position.y+SingleLineHeight, labelWidth, SingleLineHeight) 
                : new Rect(itemLevelLabelRect.xMax, secondLineY, LongLabel, SingleLineHeight);
        }

        static Rect GetQuantityRect(bool isOneLine, Rect position, float labelWidth, Rect quantityLabelRect, float secondLineY) {
            return isOneLine 
                ? new Rect(position.x + position.width - labelWidth * 2, position.y+SingleLineHeight, labelWidth - InputFieldMargin, SingleLineHeight) 
                : new Rect(quantityLabelRect.xMax + 5, secondLineY, LongLabel - InputFieldMargin, SingleLineHeight);
        }

        static Rect GetQuantityLabelRect(bool isOneLine, Rect position, float labelWidth, float secondLineX, float secondLineY) {
            return isOneLine 
                ? new Rect(position.x + position.width - labelWidth * 2, position.y, labelWidth, SingleLineHeight) 
                : new Rect(secondLineX, secondLineY, labelWidth, SingleLineHeight);
        }

        static Rect GetItemLevelLabelRect(bool isOneLine, Rect position, float labelWidth, Rect quantityRect, float secondLineY) {
            return isOneLine 
                ? new Rect(position.x + position.width - labelWidth, position.y, labelWidth, SingleLineHeight) 
                : new Rect(quantityRect.xMax + 5, secondLineY, labelWidth, SingleLineHeight);
        }

        static GUIContent GetAmountLabel(bool isShort) {
            return isShort ? new GUIContent("A", "Amount") : new GUIContent("Amount", "Amount");
        }

        static GUIContent GetQuantityLabel(bool isShort) {
            return isShort ? new GUIContent("L", "ItemLevel") : new GUIContent("Level", "Item Level");
        }

        static float ReferenceHeight(SerializedProperty property) => property.FindPropertyRelative("itemTemplateReference").Height();
        
        enum DisplayMode : byte {
            InOneLineLong,
            InOneLineShort,
            InTwoLinesLong,
            InTwoLinesShort
        }
    }
}