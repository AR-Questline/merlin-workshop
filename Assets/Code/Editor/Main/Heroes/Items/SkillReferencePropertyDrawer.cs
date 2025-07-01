using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.VSDatums;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.UI;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Heroes.Items {
    [CustomPropertyDrawer(typeof(SkillReference), true)]
    public class SkillReferencePropertyDrawer : PropertyDrawer {
        static readonly Regex CalculatedMatch = new ("(Calculated|Computable)", RegexOptions.IgnoreCase);
        List<string> _processedNames;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label) + CalculateListSize(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _processedNames ??= new List<string>();

            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            GUIUtils.PushIndent0();

            // SkillGraphRef
            Rect skillRefRect = new(position) {
                height = 2 * EditorGUIUtility.singleLineHeight
            };

            position.y += 2 * EditorGUIUtility.singleLineHeight;

            var skillGraphProp = property.FindPropertyRelative(nameof(SkillReference.skillGraphRef));
            EditorGUI.PropertyField(skillRefRect, skillGraphProp, GUIContent.none);

            if (TryDeserializeSkillGraph(skillGraphProp.FindPropertyRelative("_guid").stringValue, out var skill)) {
                var vsRect = new Rect(position);
                vsRect.height = EditorGUIUtility.singleLineHeight;
                position.y += EditorGUIUtility.singleLineHeight;
                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUI.ObjectField(vsRect, skill.EditorAsset, typeof(ScriptGraphAsset), false);
                }

                EditorGUI.indentLevel++;
                if (skill.SkillVariables.Any()) {
                    var array = property.FindPropertyRelative(nameof(SkillReference.variables));
                    DrawSkillCollection(ref position, skill.SkillVariables, array, nameof(SkillVariable.value), 1,
                        variable => new SkillVariable(variable.name, variable.value));
                }

                if (skill.SkillEnums.Any()) {
                    var array = property.FindPropertyRelative(nameof(SkillReference.enums));
                    DrawSkillCollection(ref position, skill.SkillEnums, array, nameof(SkillRichEnum.enumReference), 1,
                        skillEnum => new SkillRichEnum(skillEnum.name, new RichEnumReference(skillEnum.Value)));
                }

                if (skill.SkillAssetReferences.Any()) {
                    var array = property.FindPropertyRelative(nameof(SkillReference.assetReferences));
                    DrawSkillCollection(ref position, skill.SkillAssetReferences, array,
                        nameof(SkillAssetReference.assetReference), 3,
                        reference => new SkillAssetReference(reference.name,
                            new ShareableARAssetReference(reference.ARAssetReference)));
                }

                if (skill.SkillTemplates.Any()) {
                    var array = property.FindPropertyRelative(nameof(SkillReference.templates));
                    DrawSkillCollection(ref position, skill.SkillTemplates, array,
                        nameof(SkillTemplate.templateReference), 3,
                        template => new SkillTemplate(template.name,
                            new TemplateReference(template.templateReference?.GUID)));
                }

                if (skill.SkillDatums.Any()) {
                    var array = property.FindPropertyRelative(nameof(SkillReference.datums));
                    DrawDatumOverrides(ref position, skill, array);
                }

                EditorGUI.indentLevel--;
            }

            // clean up
            GUIUtils.PopIndent0();
            EditorGUI.EndProperty();
        }

        void DrawSkillCollection<T>(ref Rect rect, IEnumerable<T> skillCollection, SerializedProperty listProperty,
            string valuePropertyName, int height, Func<T, T> createPropertyValue) where T : IWithName {
            rect.height = EditorGUIUtility.singleLineHeight * height;
            _processedNames.Clear();

            IEnumerable<T> skillCollectionAsArray = skillCollection as T[] ?? skillCollection.ToArray();
            for (int i = 0; i < listProperty.arraySize; i++) {
                var prop = listProperty.GetArrayElementAtIndex(i);
                var name = prop.FindPropertyRelative("name").stringValue;
                if (string.IsNullOrEmpty(name)) {
                    Log.Minor?.Error("SkillReferencePropertyDrawer: The objects implementing IWithName must have a serializable name property.");
                }

                var upgradable = skillCollectionAsArray.FirstOrDefault(u => u.Name == name);
                if (upgradable == null || _processedNames.Contains(name)) {
                    // remove invalid ones
                    int oldSize = listProperty.arraySize;
                    listProperty.DeleteArrayElementAtIndex(i);
                    if (listProperty.arraySize == oldSize) {
                        listProperty.DeleteArrayElementAtIndex(i);
                    }

                    continue;
                }
                
                DrawProperty(rect,valuePropertyName, name, prop);
                _processedNames.Add(name);

                rect.y += EditorGUIUtility.singleLineHeight * height;
            }

            if (listProperty.GetPropertyValue() is List<T> serializedSkills) {
                var skillsToSerialize = new List<T>(serializedSkills);
                var newSkills = skillCollectionAsArray.Where(up => !_processedNames.Contains(up.Name)).Select(createPropertyValue.Invoke);
                skillsToSerialize.AddRange(newSkills);

                var sorted = skillsToSerialize.OrderBy(p => skillCollectionAsArray.Select(w => w.Name).IndexOf(p.Name));
                serializedSkills.Clear();
                serializedSkills.AddRange(sorted);
            }
        }

        static void DrawProperty(Rect rect, string valuePropertyName, string name, SerializedProperty prop) {
            bool isMatch = CalculatedMatch.IsMatch(name);
            var propertyRect = AllocateRectsForExtras(rect, out Rect copyButton, out Rect nameRect, out Rect equalsRect);
            var value = prop.FindPropertyRelative(valuePropertyName);
            
            DrawCopyButton(copyButton, name);
            
            EditorGUI.BeginDisabledGroup(isMatch);
            DrawLabel(nameRect, name);
            DrawEqualSign(equalsRect);

            EditorGUI.PropertyField(propertyRect, value, GUIContent.none);
            EditorGUI.EndDisabledGroup();
        }

        void DrawDatumOverrides(ref Rect rect, SkillGraph skill, SerializedProperty list) {
            rect.height = EditorGUIUtility.singleLineHeight;
            _processedNames.Clear();

            for (int i = 0; i < list.arraySize; i++) {
                var prop = list.GetArrayElementAtIndex(i);
                if (prop.boxedValue is not SkillDatum datum) {
                    continue;
                }

                var name = datum.name;

                if (!skill.SkillDatums.TryGetFirst(d => d.name == name, out var sourceDatum)) {
                    // remove invalid ones
                    int oldSize = list.arraySize;
                    list.DeleteArrayElementAtIndex(i);
                    if (list.arraySize == oldSize) {
                        list.DeleteArrayElementAtIndex(i);
                    }

                    i--;
                    continue;
                }

                bool typeChanged = !datum.type.Equals(sourceDatum.type);
                if (typeChanged) {
                    datum.type = sourceDatum.type;
                    datum.value = sourceDatum.value;
                }

                // calculate rects
                Rect valueRect = DrawPropertyLabelWithExtras(rect, name);
                VSDatumValueDrawer.Draw(valueRect, prop, datum.type, ref datum.value, out var valueChanged);

                if (typeChanged || valueChanged) {
                    prop.boxedValue = datum;
                    GUI.changed = true;
                }

                _processedNames.Add(name);

                rect.y += EditorGUIUtility.singleLineHeight;
            }

            if (list.GetPropertyValue() is List<SkillDatum> overrides) {
                var skillsToSerialize = new List<SkillDatum>(overrides);
                var newSkills = skill.SkillDatums.Where(up => !_processedNames.Contains(up.name)).Select(upgradable => upgradable.Copy());
                skillsToSerialize.AddRange(newSkills);

                var sorted = skillsToSerialize.OrderBy(p => skill.SkillDatums.Select(w => w.name).IndexOf(p.name));
                overrides.Clear();
                overrides.AddRange(sorted);
            }
        }

        static Rect DrawPropertyLabelWithExtras(Rect rect, string name) {
            var rects = AllocateRectsForExtras(rect, out Rect copyButton, out Rect nameRect, out Rect equalsRect);
            
            DrawCopyButton(copyButton, name);
            DrawLabel(nameRect, name);
            DrawEqualSign(equalsRect);
            return rects;
        }
        
        static Rect AllocateRectsForExtras(Rect rect, out Rect copyButton, out Rect nameRect, out Rect equalsRect) {
            var rects = new PropertyDrawerRects(rect);
        
            copyButton = rects.AllocateLeft(25);
            nameRect = rects.AllocateLeft(200);
            rects.LeaveSpace(5);
            equalsRect = rects.AllocateLeft(25);
            rects.LeaveSpace(5);
            return (Rect)rects;
        }

        static void DrawCopyButton(Rect rect, string name) {
            if (GUI.Button(rect, "⇒")) {
                GUIUtility.systemCopyBuffer = name;
            }
        }

        static void DrawLabel(Rect rect, string name) => EditorGUI.LabelField(rect, name);
        static void DrawEqualSign(Rect rect) => EditorGUI.LabelField(rect, "=");
        
        // === Helpers
        static float CalculateListSize(SerializedProperty property) {
            var listHeight = 0f;

            var skillGraphProp = property.FindPropertyRelative("skillGraphRef");
            string templateGUID = skillGraphProp.FindPropertyRelative("_guid").stringValue;
            if (TryDeserializeSkillGraph(templateGUID, out var skill)) {
                // Height for readonly VS object 
                listHeight += EditorGUIUtility.singleLineHeight;

                if (skill.SkillVariables.Any()) {
                    listHeight += skill.SkillVariables.Count() * EditorGUIUtility.singleLineHeight;
                }

                if (skill.SkillEnums.Any()) {
                    listHeight += skill.SkillEnums.Count() * EditorGUIUtility.singleLineHeight;
                }

                if (skill.SkillDatums.Any()) {
                    listHeight += skill.SkillDatums.Count() * (EditorGUIUtility.singleLineHeight * 2);
                }

                if (skill.SkillTemplates.Any()) {
                    listHeight += skill.SkillTemplates.Count() * (EditorGUIUtility.singleLineHeight * 3);
                }

                if (skill.SkillAssetReferences.Any()) {
                    listHeight += skill.SkillAssetReferences.Count() * (EditorGUIUtility.singleLineHeight * 3);
                }
            }

            return listHeight + EditorGUIUtility.singleLineHeight;
        }

        public static bool TryDeserializeSkillGraph(string templateRefGUID, out SkillGraph skillDef) {
            if (string.IsNullOrEmpty(templateRefGUID)) {
                skillDef = null;
                return false;
            }

            try {
                skillDef = TemplatesUtil.Load<SkillGraph>(templateRefGUID);
                return skillDef != null;
            } catch {
                skillDef = null;
                return false;
            }
        }
    }
}