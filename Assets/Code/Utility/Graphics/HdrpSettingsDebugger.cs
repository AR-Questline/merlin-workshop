using System;
using System.Reflection;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.Utility.Graphics {
    public class HdrpSettingsDebugger : UGUIWindowDisplay<HdrpSettingsDebugger> {
        static readonly OnDemandCache<Type, FieldInfo[]> FieldsCache = new OnDemandCache<Type, FieldInfo[]>(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public));

        protected override void DrawWindow() {
            var pipeline = QualitySettings.renderPipeline;
            var hdrpAsset = (HDRenderPipelineAsset)pipeline;
            var settings = hdrpAsset.currentPlatformRenderPipelineSettings;

            GUILayout.Label($"HDRP Settings Debugger - {hdrpAsset.name}");

            object boxedSettings = settings;
            var changeScope = new TGGUILayout.CheckChangeScope();

            DrawFields(boxedSettings);

            if (changeScope) {
                hdrpAsset.currentPlatformRenderPipelineSettings = (RenderPipelineSettings)boxedSettings;
            }
            changeScope.Dispose();
        }

        void DrawFields(object targetObject) {
            if (targetObject == null) {
                return;
            }
            foreach (var member in FieldsCache[targetObject.GetType()]) {
                if (!SearchContext.HasSearchInterest(member.Name)) {
                    continue;
                }
                var fieldChangeScope = new TGGUILayout.CheckChangeScope();
                var value = member.GetValue(targetObject);
                if (member.FieldType == typeof(bool)) {
                    value = TGGUILayout.Toggle(member.Name, (bool)value);
                } else if (member.FieldType == typeof(int)) {
                    value = TGGUILayout.IntField(member.Name, (int)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(float)) {
                    value = TGGUILayout.FloatField(member.Name, (float)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector2)) {
                    value = TGGUILayout.Vector2Field(member.Name, (Vector2)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector3)) {
                    value = TGGUILayout.Vector3Field(member.Name, (Vector3)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector4)) {
                    value = TGGUILayout.Vector4Field(member.Name, (Vector4)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector2Int)) {
                    value = TGGUILayout.Vector2IntField(member.Name, (Vector2Int)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector3Int)) {
                    value = TGGUILayout.Vector3IntField(member.Name, (Vector3Int)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector2)) {
                    value = TGGUILayout.Vector2Field(member.Name, (Vector2)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector3)) {
                    value = TGGUILayout.Vector3Field(member.Name, (Vector3)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector4)) {
                    value = TGGUILayout.Vector4Field(member.Name, (Vector4)member.GetValue(targetObject));
                } else if (member.FieldType == typeof(Vector2Int)) {
                    value = TGGUILayout.Vector2IntField(member.Name, (Vector2Int)member.GetValue(targetObject));
                } else if (member.FieldType.IsEnum) {
                    // Check if the enum is a flag or not
                    var isFlag = member.FieldType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
                    value = TGGUILayout.EnumField(member.Name, (Enum)member.GetValue(targetObject), isFlag);
                } else {
                    GUILayout.Label(member.Name);
                    TGGUILayout.BeginIndent();
                    DrawFields(value);
                    TGGUILayout.EndIndent();

                }
                if (fieldChangeScope) {
                    member.SetValue(targetObject, value);
                }
                fieldChangeScope.Dispose();
            }
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowHdrpSettingsDebugWindow() {
            HdrpSettingsDebugger.Toggle(UGUIWindowUtils.WindowPosition.BottomLeft);
        }

        bool IsDebugWindowShown() => HdrpSettingsDebugger.IsShown;
    }
}