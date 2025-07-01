using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates {
    [CustomPropertyDrawer(typeof(StatusStatsValues.StatusStatValue), true)]
    public class StatusStatValueDrawer : PropertyDrawer {
        const string StatusAssetsPath = "Assets/Data/Templates/Statuses";
        const int Rows = 5;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label) * Rows;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Collect Data
            var threshold = property.FindPropertyRelative("buildupThreshold");
            var thresholdEnum = (StatusStatsValues.StatusBuildupThreshold) threshold.intValue;
            var effect = property.FindPropertyRelative("effectStrength");
            var effectEnum = (StatusStatsValues.StatusEffectModifier) effect.intValue;
            
            string name = "";
            string description = "";
            string useCases = "";
            var buildupName = property.FindPropertyRelative("buildupName");
            if (buildupName != null) {
                string nameValue = buildupName.stringValue;
                if (!nameValue.IsNullOrWhitespace()) {
                    var keyword = GetKeyword(nameValue);
                    name = keyword.Name.ToString();
                    description = keyword.Description.ToString();
                    useCases = GetUseCases(nameValue);
                }
            }
            
            EditorGUI.BeginProperty(position, label, property);
            
            // Remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            int enumNameWidth = 112;
            int amountOfSpaces = 14;
            int allocatedSize = enumNameWidth * 2 + amountOfSpaces;
            var enumWidth = (position.width - allocatedSize) / 2f;
            
            Rect titleRect = position;
            Rect enumRect = position;
            Rect descRect = position;
            Rect usesRect = position;
            titleRect.height *= 1.5f / Rows;
            enumRect.height *= 1f / Rows;
            descRect.height *= 1.5f / Rows;
            usesRect.height *= 1f / Rows;
            float height = GetPropertyHeight(property, label);
            enumRect.y += 1.5f / Rows * height;
            descRect.y += 2.5f / Rows * height;
            usesRect.y += 4f / Rows * height;

            PropertyDrawerRects fullEnumRect = enumRect;

            // Threshold
            fullEnumRect.LeaveSpace(1);
            Rect thresholdTitleRect = fullEnumRect.AllocateLeft(enumNameWidth);
            fullEnumRect.LeaveSpace(1);
            Rect thresholdEnumRect = fullEnumRect.AllocateLeft(enumWidth);
            
            fullEnumRect.LeaveSpace(10);

            // Effect
            Rect effectTitleRect = fullEnumRect.AllocateLeft(enumNameWidth);
            fullEnumRect.LeaveSpace(1);
            Rect effectEnumRect = fullEnumRect.AllocateLeft(enumWidth);
            fullEnumRect.LeaveSpace(1);
            
            // Style
            var titleStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            var centeredStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter
            };
            
            // Draw fields
            EditorGUI.LabelField(titleRect, name, titleStyle);
            
            EditorGUI.LabelField(thresholdTitleRect, $"Threshold. ({StatusStatsValues.GetThreshold(thresholdEnum,0)})");
            EditorGUI.PropertyField(thresholdEnumRect, threshold, GUIContent.none);
            EditorGUI.LabelField(effectTitleRect, $"Effect Str. ({StatusStatsValues.GetModifier(effectEnum):P0})");
            EditorGUI.PropertyField(effectEnumRect, effect, GUIContent.none);
            
            EditorGUI.LabelField(descRect, description, centeredStyle);
            EditorGUI.LabelField(usesRect, useCases, centeredStyle);

            // Clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        Keyword GetKeyword(string name) {
            var field = typeof(Keyword).GetField($"Status{name}");
            if (field != null) {
                return (field.GetValue(null) as Keyword);
            } 
            return null;
        }

        string GetUseCases(string name) {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] {StatusAssetsPath});
            string result = "Used In: ";
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) {
                    continue;
                }
                var buildup = prefab.GetComponentInChildren<BuildupAttachment>(true);
                if (buildup == null) {
                    continue;
                }
                if (!buildup.BuildupStatusType?.EnumName.Equals(name) ?? false) {
                    continue;
                }
                result += $"{prefab.name}, ";
            }
            return result;
        }
    }
}