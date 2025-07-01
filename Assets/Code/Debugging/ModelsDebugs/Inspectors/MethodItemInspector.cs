using System.Linq;
using System.Reflection;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    public class MethodItemInspector {
        public static bool nicifyModelsDebug = false;
        public static string classMethodSeparator;
        static string s_expanded; 
        
        public void Draw(MethodMemberListItem method, object target, string[] searchContext, MethodInfo methodState = null, bool marvinVisual = false) {
            if (!method.Name.ContainsAny(searchContext)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = method.Callable;
            TGGUILayout.BeginHorizontal();
            GUI.backgroundColor = methodState != null
                                  && methodState.Invoke(target, new object[] { }) is true
                                      ? Color.green
                                      : Color.white;

            string methodName = method.Name;
            methodName = NameNicification(methodName, marvinVisual);

            if (GUILayout.Button(methodName, marvinVisual ? TGGUILayout.ButtonStyleMarvin : TGGUILayout.ButtonStyle)) {
                var result = method.TryCall(target);
                Log.Important?.Info($"Method called: {method.Name}; result {result}");
            }

            bool hasParameters = (method.Parameters?.Length ?? 0) > 0;
            string methodSignature = hasParameters
                ? $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))})" 
                : method.Name;
            if (hasParameters) {
                if (s_expanded == methodSignature) {
                    if (GUILayout.Button("▼", GUILayout.Width(50))) {
                        s_expanded = null;
                    }
                } else {
                    if (GUILayout.Button("▶", GUILayout.Width(50))) {
                        s_expanded = methodSignature;
                    }
                }
            }
            TGGUILayout.EndHorizontal();
            
            if (s_expanded == methodSignature) {
                if (method.Parameters != null) {
                    foreach (Parameter parameter in method.Parameters) {
                        if (!parameter.IsPrimitive && !parameter.IsString) {
                            continue;
                        }

                        TGGUILayout.BeginHorizontal();

                        GUILayout.Label(parameter.Name, GUILayout.ExpandWidth(false));
                        if (parameter.ParameterType == typeof(bool)) {
                            DrawBoolParameter(parameter);
                        } else if (parameter.ParameterType == typeof(float)) {
                            DrawFloatParameter(parameter);
                        } else {
                            DrawDefaultParameter(parameter);
                        }
                        TGGUILayout.EndHorizontal();
                    }
                }
            }
            
            GUI.enabled = oldEnable;
        }
        
        static string NameNicification(string methodName, bool removeMethodPrefix) {

#if UNITY_EDITOR
            if (removeMethodPrefix) {
                methodName = methodName[(methodName.IndexOf('.') + 1)..];
                methodName = UnityEditor.ObjectNames.NicifyVariableName(methodName);
            } else if (nicifyModelsDebug) {
                methodName = UnityEditor.ObjectNames.NicifyVariableName(methodName);
            }

            if (classMethodSeparator != ".") {
                methodName = methodName.Replace(".", classMethodSeparator);
            }
#else
            if (removeMethodPrefix) {
                methodName = methodName[(methodName.IndexOf('.') + 1)..];
            }
#endif
            return methodName;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void DrawLabel(string text) {
            GUILayout.Label(text, TGGUILayout.LabelStyle);
        }
        
        static void DrawBoolParameter(Parameter parameter) {
            bool value = parameter.Value as bool? ?? false;
            parameter.Value = GUILayout.Toggle(value, "");
        }
        
        static void DrawFloatParameter(Parameter parameter) {
            float value = parameter.Value as float? ?? 0f;
            string newValue = GUILayout.TextField(value.ToString(".0###########"));
            if (newValue.EndsWith('.')) {
                newValue += "0";
            }
            if (float.TryParse(newValue, out var newFloat)) {
                parameter.Value = newFloat;
            }
        }

        static void DrawDefaultParameter(Parameter parameter) {
            string newValue = GUILayout.TextField(parameter.Value?.ToString() ?? "");
            try {
                var newValueCasted = System.Convert.ChangeType(newValue, parameter.ParameterType);
                parameter.Value = newValueCasted;
            } catch {
                parameter.Value = newValue;
                var oldColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("Invalid", GUILayout.ExpandWidth(false), GUILayout.Width(150));
                GUI.color = oldColor;
            }
        }
    }
}