using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Heroes.Items;
using Awaken.TG.Editor.Utility.VSDatums;
using Awaken.TG.Main.Heroes.Skills.Graphs.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using XNode.Attributes;
using XNodeEditor;
using Node = XNode.Node;
using Object = UnityEngine.Object;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using static Awaken.TG.MVC.Attributes.AttributesCache;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Utility.StoryGraphs {
    public static class NodeGUIUtil {
        // === Cache
        static OnDemandCache<Type, FieldInfo[]> s_typeFields = new(TypeFieldsFactory);
        
        static Node Node(SerializedProperty property) => property.serializedObject.targetObject as Node;
        
        public static NodeGraph Graph(SerializedProperty property) => Graph(property.serializedObject.targetObject);
        public static NodeGraph Graph(Object obj) {
            if (obj is Node node) {
                return node.graph;
            } else if (obj is NodeElement element) {
                return element.genericParent?.Graph;
            } else if (obj is NodeGraph graph) {
                return graph;
            } else {
                return null;
            }
        }

        // defined drawers for field types / attributes
        delegate void FieldDrawer(SerializedProperty prop, FieldInfo field, int width);
        delegate void FieldAttributeDrawer(SerializedProperty prop, FieldInfo field, Attribute genericAttribute, int width);
        
        static readonly (Type, FieldDrawer)[] TypeDrawers = {
            (typeof(LocString), DrawLocString),
            (typeof(OrderablePortAttribute), DrawOrderablePort),
            (typeof(StringPopupAttribute), DrawStringsPopup),
            (typeof(Node.InputAttribute), DrawPort),
            (typeof(Node.OutputAttribute), DrawPort),
            (typeof(TextAreaAttribute), DrawTextArea),
            (typeof(TagsAttribute), DrawTags),
            (typeof(ListAttribute), DrawList),
            (typeof(SkillReference), DrawSkillReference)
        };
        
        static readonly (Type, FieldAttributeDrawer)[] AttributeDrawers = {
            (typeof(InfoBoxAttribute), DrawInfoBox),
            (typeof(ObsoleteAttribute), DrawObsolete),
        };
        
        static void DrawAttributeDrawers(SerializedProperty property, FieldInfo field, int width, Attribute[] attributes) {
            foreach (var attribute in attributes) {
                foreach (var (type, drawer) in AttributeDrawers) {
                    if (type.IsInstanceOfType(attribute)) {
                        drawer.Invoke(property, field, attribute, width);
                        break;
                    }
                }
            }
        }
        
        static void DrawTypeDrawer(SerializedProperty property, FieldInfo field, int width, Attribute[] attributes) {
            var fieldType = field.FieldType;
            foreach (var (type, drawer) in TypeDrawers) {
                foreach (var attribute in attributes) {
                    if (type.IsInstanceOfType(attribute)) {
                        drawer.Invoke(property, field, width);
                        return;
                    }
                }
                if (type.IsAssignableFrom(fieldType)) {
                    drawer.Invoke(property, field, width);
                    return;
                }
            }
            // fallback
            EditorGUILayout.PropertyField(property, GuiContent(field, attributes), true);
        }

        static bool ShouldShow(SerializedProperty property, Attribute[] attributes) {
            foreach (var attribute in attributes) {
                if (attribute is ShowIfAttribute showIf) {
                    if (!GetBoolMember(property, showIf.Condition)) {
                        return false;
                    }
                } else if (attribute is HideIfAttribute hideIf) {
                    if (GetBoolMember(property, hideIf.Condition)) {
                        return false;
                    }
                }
            }

            return true;
        }
        
        static bool GetBoolMember(SerializedProperty property, string visibleIfName, bool emptyNameFallback = true, bool noMethodFallback = false) {
            const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (string.IsNullOrWhiteSpace(visibleIfName)) {
                return emptyNameFallback;
            }

            var obj = property.serializedObject.targetObject;
            if (obj == null) {
                return noMethodFallback;
            }
                
            var type = obj.GetType();
            if (type.GetProperty(visibleIfName, BindingFlags | BindingFlags.GetProperty) is { } propertyInfo) {
                var result = propertyInfo.GetValue(obj);
                return result is true;
            }
            if (type.GetField(visibleIfName, BindingFlags | BindingFlags.GetField) is { } fieldInfo) {
                var result = fieldInfo.GetValue(obj);
                return result is true;
            }
            if (type.GetMethod(visibleIfName, BindingFlags | BindingFlags.InvokeMethod) is { } methodInfo) {
                var result = methodInfo.Invoke(obj, Array.Empty<object>());
                return result is true;
            }

            return noMethodFallback;
        }

        public static void DrawNodePropertiesExcept(SerializedObject serialized, Node node, ICollection<string> omittedProps) {
            FieldInfo[] fields = s_typeFields[serialized.targetObject.GetType()];

            foreach (FieldInfo field in fields) {
                if (omittedProps.Contains(field.Name)) {
                    continue;
                }

                int nodeWidth = NodeGUIUtil.GetNodeWidth(node);
                SerializedProperty serializedProperty = serialized.FindProperty(field.Name);
                DrawProperty(serializedProperty, field, nodeWidth);
            }
        }

        public static void DrawGivenProperties(SerializedObject serialized, Node node, ICollection<string> propertiesToDraw) {
            FieldInfo[] fields = s_typeFields[serialized.targetObject.GetType()];

            foreach (FieldInfo field in fields) {
                if (!propertiesToDraw.Contains(field.Name)) {
                    continue;
                }

                int nodeWidth = GetNodeWidth(node);
                SerializedProperty serializedProperty = serialized.FindProperty(field.Name);
                DrawProperty(serializedProperty, field, nodeWidth);
            }
        }

        public static void DrawProperty(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            if (serializedProperty == null || field == null) {
                return;
            }
            // don't draw when too far
            if (NodeEditorWindow.FarView) {
                return;
            }

            Attribute[] attributes = MemberAttributes[field];

            // abort if ShowIf/HideIf attributes don't allow drawing
            if (!ShouldShow(serializedProperty, attributes)) {
                return;
            }

            bool hasReadOnly = attributes.Any(a => a is ReadOnlyAttribute);
            bool wasGUIEnabled = GUI.enabled;
            if (hasReadOnly) {
                GUI.enabled = false;
            }
            
            DrawAttributeDrawers(serializedProperty, field, nodeWidth, attributes);
            DrawTypeDrawer(serializedProperty, field, nodeWidth, attributes);
            
            if (hasReadOnly) {
                GUI.enabled = wasGUIEnabled;
            }
        }

        // === Custom Attributes

        static void DrawInfoBox(SerializedProperty property, FieldInfo fieldInfo, Attribute genericAttribute, int nodeWidth) {
            InfoBoxAttribute attribute = (InfoBoxAttribute) genericAttribute;
            string infoText = attribute.Message;
            InfoMessageType infoBoxType = attribute.InfoMessageType;

            if (!GetBoolMember(property, attribute.VisibleIf)) {
                return;
            }
            
            var messageType = infoBoxType switch {
                InfoMessageType.Info => MessageType.Info,
                InfoMessageType.Warning => MessageType.Warning,
                InfoMessageType.Error => MessageType.Error,
                _ => MessageType.None
            };
            EditorGUILayout.HelpBox(infoText, messageType);
        }
        
        static void DrawObsolete(SerializedProperty property, FieldInfo fieldInfo, Attribute genericAttribute, int nodeWidth) {
            ObsoleteAttribute attribute = (ObsoleteAttribute) genericAttribute;
            GUILayout.Label($"<color=red><b>Obsolete:\n{attribute.Message}</b></color>", new GUIStyle(EditorStyles.largeLabel){richText = true, wordWrap = true});
        }

        /// <summary>
        /// LocStrings drawn in NodeGuiUtil uses I2LanguagesStory to save changes.
        /// </summary>
        static void DrawLocString(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            // header
            DrawHeader(serializedProperty, field);
            // label (optional)
            DrawLabel(field);

            LocStringData data = LocStringGUIUtils.GetData(serializedProperty, field, nodeWidth);

            // draw
            EditorGUI.BeginChangeCheck();
            int charCount = data.textString.Length;
            GUI.contentColor = charCount > 180 ? Color.red : Color.white;
            string value = EditorGUILayout.TextArea(data.textString, EditorStyles.textArea,GUILayout.Height(data.height));
            if (EditorGUI.EndChangeCheck() || data.wasChanged) {
                value = value?.Replace("\r", "");
                if (data.stringTable != null) {
                    LocalizationUtils.ChangeTextTranslation(data.id, value, data.stringTable, true);
                } else {
                    Log.Important?.Error($"Failed to find StringTable: {data.stringCollection}");
                }
            }
            GUI.contentColor = Color.white;

            try {
                SerializedProperty textLenght = serializedProperty.serializedObject.FindProperty("textLength");
                if (textLenght != null) {
                    value ??= string.Empty;
                    textLenght.intValue = value.Length;
                }
            } catch (Exception) { /* ignored */ }

            if (TGEditorPreferences.Instance.showTermsInGraphs) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("⇒", GUILayout.Width(25), GUILayout.ExpandWidth(false))) {
                    GUIUtility.systemCopyBuffer = data.id;
                }
                EditorGUILayout.LabelField(data.id);
                EditorGUILayout.EndHorizontal();
            }
        }

        static void DrawSkillReference(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            EditorGUILayout.Space();
            var skillGraphProp = serializedProperty.FindPropertyRelative("skillGraphRef");
            EditorGUILayout.PropertyField(skillGraphProp, new GUIContent(skillGraphProp.name, skillGraphProp.name), true);

            if (SkillReferencePropertyDrawer.TryDeserializeSkillGraph(skillGraphProp.FindPropertyRelative("_guid").stringValue, out var skill)) {
                if (skill.SkillVariables.Any()) {
                    // draw variable overrides
                    var array = serializedProperty.FindPropertyRelative("variables");
                    DrawVariableOverrides(skill, array);
                }
                if (skill.SkillEnums.Any()) {
                    // draw variable overrides
                    var array = serializedProperty.FindPropertyRelative("enums");
                    DrawEnumOverrides(skill, array);
                }
                if (skill.SkillDatums.Any()) {
                    var array = serializedProperty.FindPropertyRelative("datums");
                    DrawDatumOverrides(skill, array);
                }
            }
            EditorGUILayout.Space();
        }
        
        static void DrawVariableOverrides(SkillGraph skill, SerializedProperty list) {
            List<string> drawnIds = new List<string>();

            // draw existing overrides
            for (int i = 0; i < list.arraySize; i++) {
                var prop = list.GetArrayElementAtIndex(i);
                var name = prop.FindPropertyRelative("name").stringValue;

                var upgradable = skill.SkillVariables.FirstOrDefault(u => u.name == name);
                if (upgradable == null || drawnIds.Contains(name)) {
                    // remove invalid ones
                    int oldSize = list.arraySize;
                    list.DeleteArrayElementAtIndex(i);
                    if (list.arraySize == oldSize) {
                        list.DeleteArrayElementAtIndex(i);
                    }
                    continue;
                }

                var value = prop.FindPropertyRelative("value");

                EditorGUILayout.BeginHorizontal();
                // draw variable
                if (GUILayout.Button("⇒", GUILayout.Width(25), GUILayout.ExpandWidth(false))) {
                    GUIUtility.systemCopyBuffer = name;
                }
                EditorGUILayout.LabelField(new GUIContent(name, name), GUILayout.Width(100), GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField("=", GUILayout.Width(25), GUILayout.ExpandWidth(false));
                EditorGUILayout.PropertyField(value, GUIContent.none, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                drawnIds.Add(name);
            }

            // fill list with new upgradable overrides
            foreach (var upgradable in skill.SkillVariables.Where(up => !drawnIds.Contains(up.name))) {
                list.arraySize += 1;
                var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                element.FindPropertyRelative("name").stringValue = upgradable.name;
                element.FindPropertyRelative("value").floatValue = upgradable.value;
            }
        }
        
        static void DrawEnumOverrides(SkillGraph skill, SerializedProperty list) {
            List<string> drawnIds = new List<string>();

            // draw existing overrides
            for (int i = 0; i < list.arraySize; i++) {
                var prop = list.GetArrayElementAtIndex(i);
                var name = prop.FindPropertyRelative("name").stringValue;

                var enumReferenceValue = skill.SkillEnums.FirstOrDefault(u => u.name == name);
                if (enumReferenceValue == null || drawnIds.Contains(name)) {
                    // remove invalid ones
                    int oldSize = list.arraySize;
                    list.DeleteArrayElementAtIndex(i);
                    if (list.arraySize == oldSize) {
                        list.DeleteArrayElementAtIndex(i);
                    }
                    continue;
                }

                var value = prop.FindPropertyRelative("enumReference");
                
                EditorGUILayout.BeginHorizontal();
                // draw variable
                if (GUILayout.Button("⇒", GUILayout.Width(25), GUILayout.ExpandWidth(false))) {
                    GUIUtility.systemCopyBuffer = name;
                }
                EditorGUILayout.LabelField(new GUIContent(name, name), GUILayout.Width(100), GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField("=", GUILayout.Width(25), GUILayout.ExpandWidth(false));
                EditorGUILayout.PropertyField(value, GUIContent.none, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                drawnIds.Add(name);
            }

            // fill list with new upgradable overrides
            var overrides = list.GetPropertyValue() as List<SkillRichEnum>;
            if (overrides == null) return;
            foreach (var enumReferenceOverride in skill.SkillEnums.Where(up => !drawnIds.Contains(up.name))) {
                overrides.Add(new SkillRichEnum() {
                    name = enumReferenceOverride.name,
                    enumReference = new TG.Main.Utility.RichEnums.RichEnumReference(enumReferenceOverride.Value) 
                });
            }
        }

        static void DrawDatumOverrides(SkillGraph skill, SerializedProperty list) {
            List<string> drawnNames = new List<string>();

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
            
                EditorGUILayout.BeginHorizontal();
                // draw variable
                if (GUILayout.Button("⇒", GUILayout.Width(25), GUILayout.ExpandWidth(false))) {
                    GUIUtility.systemCopyBuffer = name;
                }
                EditorGUILayout.LabelField(new GUIContent(name, name), GUILayout.Width(100), GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField("=", GUILayout.Width(25), GUILayout.ExpandWidth(false));
                VSDatumValueDrawer.DrawInLayout(prop, datum.type, ref datum.value, out var valueChanged);
                EditorGUILayout.EndHorizontal();
                
                if (typeChanged || valueChanged) {
                    prop.boxedValue = datum;
                    GUI.changed = true;
                }
            
                drawnNames.Add(name);
            }
            
            // fill list with new upgradable overrides
            var overrides = (List<SkillDatum>)list.GetPropertyValue();
            foreach (var sourceDatum in skill.SkillDatums.Where(up => !drawnNames.Contains(up.name))) {
                overrides.Add(sourceDatum.Copy());
            }
        }

        static void DrawTextArea(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            // header
            DrawHeader(serializedProperty, field);
            // label (optional)
            DrawLabel(field);
            
            // setup text area
            float height = SetupTextArea(field, nodeWidth, serializedProperty.stringValue);

            // draw
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 1f);
            serializedProperty.stringValue = EditorGUILayout.TextArea(serializedProperty.stringValue,
                EditorStyles.textArea, GUILayout.Height(height));
            GUI.backgroundColor = old;
        }

        static void DrawTags(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            // header
            HeaderAttribute headerAttr = GetCustomAttribute<HeaderAttribute>(field);
            if (headerAttr != null) {
                EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
            }
            TagsEditing.Show(serializedProperty, serializedProperty.ExtractAttribute<TagsAttribute>().tagsCategory, nodeWidth);
        }

        static void DrawList(SerializedProperty serializedProperty, FieldInfo field, int _) {
            // header
            HeaderAttribute headerAttr = GetCustomAttribute<HeaderAttribute>(field);
            if (headerAttr != null) {
                EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
            }

            ListEditing.Show(serializedProperty, serializedProperty.ExtractAttribute<ListAttribute>().listEditOption);
        }
        
        static void DrawStringsPopup(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            var popupAttribute = GetCustomAttribute<StringPopupAttribute>(field);

            var parentObject = serializedProperty.GetPropertyValue(1);
            var parentType = parentObject.GetType();
            var optionsMember = parentType.GetField(popupAttribute.SourceFiledName) as MemberInfo ??
                                parentType.GetProperty(popupAttribute.SourceFiledName);
            var optionsValue = optionsMember?.MemberValue(parentObject);
            var options = (optionsValue as IEnumerable<string>)?.ToArray();

            if (options == null) {
                EditorGUILayout.HelpBox($"Can not find input for popup for field {field.Name} ({popupAttribute.SourceFiledName})", MessageType.Error);
                return;
            }

            DrawLabel(field);
            int selected = options.IndexOf(serializedProperty.stringValue);
            if (selected == -1 && !string.IsNullOrWhiteSpace(serializedProperty.stringValue)) {
                selected = 0;
                options = serializedProperty.stringValue.Yield().Concat(options).ToArray();
            }
            
            var wasChange = GUI.changed;
            GUI.changed = false;
            
            selected = EditorGUILayout.Popup(selected, options);
            selected = Mathf.Clamp(selected, 0, options.Length - 1);

            if (GUI.changed) {
                serializedProperty.stringValue = options[selected];
            }

            GUI.changed |= wasChange;
        }

        // === Ports

        static void DrawPort(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
            var port = Node(serializedProperty).GetPort((NodePort.FieldNameCompressed)field.Name);
            var fieldName = port.fieldNameCompressed.ToFieldNameString();
            var fieldNameLength = fieldName.Length;
            // Check if name ends with "Port" and removes that postfix from name
            if (fieldName.Length > 4 && fieldName[fieldNameLength - 1] == 't' && fieldName[fieldNameLength - 2] == 'r' && fieldName[fieldNameLength - 3] == 'o' &&
                (fieldName[fieldNameLength - 4] == 'p' || fieldName[fieldNameLength - 4] == 'P')) {
                fieldName = fieldName.Substring(0, fieldNameLength - 4);
            }
            var niceName = ObjectNames.NicifyVariableName(fieldName);
            var label = niceName;

            OptionalPortAttribute attribute = GetCustomAttribute<OptionalPortAttribute>(field);
            if (attribute != null) {
                if (port.IsConnected) {
                    label += $"  <color=#C0C0C0FF><i><size=10>*{attribute.DefaultValue}*</size></i></color>";
                } else {
                    label += $"  <color=yellow><b>*{attribute.DefaultValue}*</b></color>";
                }
            }
            NodeEditorGUILayout.PortField(new GUIContent(label, niceName), port);
        }

        public static void SmallPortField(Vector2 position, NodePort port, bool faded) {
            if (port == null) return;

            Rect rect = new Rect(position, new Vector2(10, 10));

            NodeEditor editor = NodeEditor.GetEditor(port.node, NodeEditorWindow.current);
            Color backgroundColor = editor.GetTint();
            Color col = NodeEditorWindow.current.graphEditor.GetPortColor(port);
            if (faded) {
                col *= 0.5f;
                col.a *= 0.8f;
            }

            NodeEditorGUILayout.DrawPortHandle(rect, backgroundColor, col);

            // Register the handle position
            Vector2 portPos = rect.center;
            NodeEditor.portPositions[port] = portPos;
        }

        static void DrawOrderablePort(SerializedProperty prop, FieldInfo field, int width) {
            var node = Node(prop);
            DrawInputPortAndOrder(node, node.Ports.FirstOrDefault( p => p.fieldName == prop.name));
        }
        
        public static void DrawInputPortAndOrder(Node node, NodePort port) {
            string portLabel = port.fieldName;
            int childCount = 0;
            int order = -1;

            var targetedPorts = port.IsInput ? node.Inputs : node.Outputs;
            var parentsPorts = targetedPorts.SelectMany( i => i.GetConnections() ).ToList();
            var parentPort = parentsPorts.FirstOrDefault();

            if (parentsPorts.Count == 1 && parentPort != null && parentPort.ConnectionCount > 1) {
                order = parentPort.GetConnectionIndex(port);
                childCount = parentPort.ConnectionCount;
                portLabel = $"<b>No. {(order + 1).ToString()}</b> {port.fieldName}";
            } else if (parentPort == null) {
                portLabel = "<b>Not connected</b> {port.fieldName}";
            }
            
            EditorGUILayout.BeginHorizontal();

            // input port
            NodeEditorGUILayout.PortField(new GUIContent(portLabel, portLabel), port);

            // move up button
            GUI.enabled = order >= 1;
            if (GUILayout.Button(new GUIContent("\u25b2", "move up"), EditorStyles.miniButton, GUILayout.Width(30)) && parentPort != null) {
                List<Node> oldList = parentPort.GetConnections().Select(c => c.node).ToList();
                List<Node> newList = new List<Node>(oldList);
                newList.Swap(order, order - 1);
                parentPort.Redirect(oldList, newList);
            }

            // move down button
            GUI.enabled = order < childCount - 1;
            if (GUILayout.Button(new GUIContent("\u25bc", "move down"), EditorStyles.miniButton, GUILayout.Width(30)) && parentPort != null) {
                List<Node> oldList = parentPort.GetConnections().Select(c => c.node).ToList();
                List<Node> newList = new List<Node>(oldList);
                newList.Swap(order, order + 1);
                parentPort.Redirect(oldList, newList);
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        
        // === Helpers
        
        static void DrawHeader(SerializedProperty serializedProperty, FieldInfo field) {
            HeaderAttribute headerAttr = GetCustomAttribute<HeaderAttribute>(field);
            if (headerAttr != null) {
                EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
            } else if(field == null){
                EditorGUILayout.LabelField(serializedProperty.displayName, EditorStyles.boldLabel);
            }
        }

        static void DrawLabel(FieldInfo field) {
            LabelTextAttribute attribute = GetCustomAttribute<LabelTextAttribute>(field);
            var tooltip = GetCustomAttribute<TooltipAttribute>(field)?.tooltip ?? attribute?.Text;
            if (attribute != null) {
                var text = attribute.Text;
                var label = new GUIContent(text, tooltip);
                EditorGUILayout.LabelField(label);
            }
        }
        
        public static float SetupTextArea(FieldInfo fieldInfo, int nodeWidth, string textContent) {
            // retrieve attribute
            TextAreaAttribute attribute = GetCustomAttribute<TextAreaAttribute>(fieldInfo);
            
            // setup gui style
            GUIStyle style = EditorStyles.textArea;
            style.wordWrap = true;
            
            // calculate height of text area
            var content = new GUIContent(textContent);
            var nodeStyle = NodeEditorResources.styles.nodeBody;
            float height = style.CalcHeight(content, nodeWidth - (nodeStyle.padding.horizontal + nodeStyle.margin.horizontal + 1));
            if (attribute != null) {
                float requiredHeight = height;
                float minimumHeight = attribute.minLines * EditorGUIUtility.singleLineHeight;
                float maximumHeight = attribute.maxLines * EditorGUIUtility.singleLineHeight;
                height = M.Mid(minimumHeight, maximumHeight, requiredHeight);
            }
            return height;
        }
        
        static GUIContent GuiContent(FieldInfo field, Attribute[] attributes) {
            if (attributes.Any(attribute => attribute is HideLabelAttribute)) {
                return GUIContent.none;
            }
            var label = attributes.OfType<LabelTextAttribute>().FirstOrDefault()?.Text ?? StringUtil.NicifyName(field.Name);
            var tooltip = attributes.OfType<TooltipAttribute>().FirstOrDefault()?.tooltip ?? $"{label} "; // add space to avoid tooltip being cut off
            return GUIUtils.Content(label, tooltip);
        }
        
        public static int GetNodeWidth(Node node) {
            var editor = NodeEditor.GetEditor(node, NodeEditorWindow.current);
            return editor?.GetWidth() ?? 200;
        }

        static FieldInfo[] TypeFieldsFactory(Type type) {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => GetCustomAttribute<HideInInspector>(f) == null)
                .ToArray();
        }
    }
}