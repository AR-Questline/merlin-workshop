using System;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.MoreGUI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums {
    [CustomPropertyDrawer(typeof(VSDatumType))]
    public class VSDatumTypeDrawer : PropertyDrawer {
        static readonly GUIContent[] Names;
        static readonly GUIContent[] NiceNames;
        static readonly VSDatumType[] Types;
        
        static VSDatumTypeDrawer() {
            GetAll(out Names, out NiceNames, out Types);
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Draw(position, (VSDatumType)property.boxedValue, out var type, out var changed);
            if (changed) {
                property.boxedValue = type;
                GUI.changed = true;
            }
        }

        public static void Draw(Rect rect, in VSDatumType inType, out VSDatumType outType, out bool changed) {
            int index = Types.IndexOf(inType);
            int newIndex = AREditorPopup.Draw(rect, index, Names, NiceNames);
            changed = newIndex != index;
            outType = Types[newIndex];
        }
        
        static void GetAll(out GUIContent[] names, out GUIContent[] niceNames, out VSDatumType[] types) {
            var generalValues = Enum.GetValues(typeof(VSDatumGeneralType));
            var richEnumValues = Enum.GetValues(typeof(VSDatumRichEnumType));
            var enumValues = Enum.GetValues(typeof(VSDatumEnumType));
            var assetValues = Enum.GetValues(typeof(VSDatumAssetType));
            int count = generalValues.Length + (enumValues.Length - 1) + (assetValues.Length - 1) + (richEnumValues.Length - 1);
            names = new GUIContent[count];
            niceNames = new GUIContent[count];
            types = new VSDatumType[count];

            int index = 0;
            for (int i = 0; i < generalValues.Length; i++) {
                var generalType = (VSDatumGeneralType)generalValues.GetValue(i);
                if (generalType is VSDatumGeneralType.Enum or VSDatumGeneralType.Asset or VSDatumGeneralType.RichEnum) {
                    continue;
                }
                names[index] = new GUIContent(generalType.ToString());
                niceNames[index] = names[index];
                types[index] = new VSDatumType(generalType, 0);
                index++;
            }
            for (int i = 0; i < richEnumValues.Length; i++) {
                var enumType = (VSDatumRichEnumType) richEnumValues.GetValue(i);
                var enumName = enumType.ToString();
                names[index] = new GUIContent($"RichEnum/{enumName}");
                niceNames[index] = new GUIContent(enumName);
                types[index] = new VSDatumType(VSDatumGeneralType.RichEnum, (byte)enumType);
                index++;
            }
            for (int i = 0; i < enumValues.Length; i++) {
                var enumType = (VSDatumEnumType)enumValues.GetValue(i);
                var enumName = enumType.ToString();
                names[index] = new GUIContent($"Enum/{enumName}");
                niceNames[index] = new GUIContent(enumName);
                types[index] = new VSDatumType(VSDatumGeneralType.Enum, (byte)enumType);
                index++;
            }
            for (int i = 0; i < assetValues.Length; i++) {
                var assetType = (VSDatumAssetType)assetValues.GetValue(i);
                var assetName = assetType.ToString();
                names[index] = new GUIContent($"Asset/{assetName}");
                niceNames[index] = new GUIContent(assetName);
                types[index] = new VSDatumType(VSDatumGeneralType.Asset, (byte)assetType);
                index++;
            }
        }
    }
}