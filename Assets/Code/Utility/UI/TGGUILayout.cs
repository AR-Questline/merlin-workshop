using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Awaken.Utility.UI {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public static class TGGUILayout {
        public static GUIStyle LabelStyle { get; }
        public static GUIStyle WhiteBackgroundStyle { get; }

        static GUIStyle s_buttonStyle;
        static GUIStyle s_buttonStyleMarvin;
        static GUIStyle s_boldLabelStyle;
        
        public static GUIStyle ButtonStyle {
            get {
                CacheStyleWithChanges(ref s_buttonStyle);
                return s_buttonStyle;
            }
        }
        public static GUIStyle ButtonStyleMarvin {
            get {
                CacheStyleWithChanges(ref s_buttonStyleMarvin);
                return s_buttonStyleMarvin;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static GUIStyle BoldLabel {
            get {
                s_boldLabelStyle ??= new GUIStyle(LabelStyle) {
                    fontStyle = FontStyle.Bold
                };
                return s_boldLabelStyle;
            }
        }

        static int s_guiLayout = 0;

        static TGGUILayout() {
            LabelStyle = new GUIStyle(GUI.skin.label) { richText = true };
            Color[] pixels = {Color.white};
            Texture2D tex = new(1, 1);
            tex.SetPixels(pixels);
            tex.Apply();
            WhiteBackgroundStyle = new GUIStyle();
            WhiteBackgroundStyle.normal.background = tex;
        }

        public static void BeginGUILayout() {
            s_guiLayout++;
        }
        public static void EndGUILayout() {
            s_guiLayout--;
        }

        #region Drawers
        public static bool Toggle(string label, bool value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Toggle(label, value);
            }
#endif
            return GUILayout.Toggle(value, label);
        }
        
        public static Object ObjectField(string label, Object unityObject, Type type, bool allowSceneObject) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.ObjectField(label, unityObject, type, allowSceneObject);
            }
#endif
            GUILayout.Label($"{label}: {unityObject.name} <{type.Name}>");
            return unityObject;
        }

        public static float FloatField(string label, float value, params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.FloatField(label, value, options);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );

            bool wasChanged = GUI.changed;
            if (options.Length == 0) {
                options = new[] {GUILayout.ExpandWidth(true)};
            }
            string newValue = GUILayout.TextField(value.ToString(CultureInfo.InvariantCulture), options);
            if (float.TryParse(newValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var result) && Math.Abs(result - value) > 0.0001f) {
                value = result;
                GUI.changed = true;
            } else {
                GUI.changed = wasChanged;
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static float DelayedFloatField(string label, float value, params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedFloatField(label, value, options);
            }
#endif
            //TODO: Real delayed field
            return FloatField(label, value, options);
        }

        public static float DelayedFloatField(float value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedFloatField(value);
            }
#endif
            //TODO: Real delayed field
            return FloatField("", value);
        }
        
        
        public static int IntField(string label, int value, bool expanded = true) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.IntField(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );

            bool wasChanged = GUI.changed;
            string newValue = GUILayout.TextField(value.ToString(CultureInfo.InvariantCulture), GUILayout.ExpandWidth(expanded));
            if (int.TryParse(newValue, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var result) && result != value) {
                value = result;
                GUI.changed = true;
            } else {
                GUI.changed = wasChanged;
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static int DelayedIntField(string label, int value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedIntField(label, value);
            }
#endif
            //TODO: Real delayed field
            return IntField(label, value);
        }

        public static int DelayedIntField(int value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedIntField(value);
            }
#endif
            //TODO: Real delayed field
            return IntField("",value);
        }
        
        
        public static string TextField(string label, string value, params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.TextField(label, value, options);
            }
#endif
            GUILayout.BeginHorizontal(options);
            GUILayout.Label( label, LabelStyle, GUILayout.ExpandWidth(false) );
            value = GUILayout.TextField(value, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return value;
        }
        public static string DelayedTextField(string label, string value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedTextField(label, value);
            }
#endif
            //TODO: Real delayed field
            return TextField(label, value);
        }

        public static string DelayedTextField(string value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.DelayedTextField(value);
            }
#endif
            //TODO: Real delayed field
            return TextField("", value);
        }
        
        
        public static Vector2 Vector2Field(string label, Vector2 value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Vector2Field(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );
            value.x = FloatField("x", value.x);
            value.y = FloatField("y", value.y);
            GUILayout.EndHorizontal();
            return value;
        }
        
        public static Vector3 Vector3Field(string label, Vector3 value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Vector3Field(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );
            value.x = FloatField("x", value.x);
            value.y = FloatField("y", value.y);
            value.z = FloatField("z", value.z);
            GUILayout.EndHorizontal();
            return value;
        }
        public static Vector4 Vector4Field(string label, Vector4 value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Vector4Field(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );
            value.x = FloatField("x", value.x);
            value.y = FloatField("y", value.y);
            value.z = FloatField("z", value.z);
            value.w = FloatField("w", value.w);
            GUILayout.EndHorizontal();
            return value;
        }
        
        public static Vector2Int Vector2IntField(string label, Vector2Int value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Vector2IntField(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );
            value.x = IntField("x", value.x);
            value.y = IntField("y", value.y);
            GUILayout.EndHorizontal();
            return value;
        }
        public static Vector3Int Vector3IntField(string label, Vector3Int value) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Vector3IntField(label, value);
            }
#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label( label, GUILayout.ExpandWidth(false) );
            value.x = IntField("x", value.x);
            value.y = IntField("y", value.y);
            value.z = IntField("z", value.z);
            GUILayout.EndHorizontal();
            return value;
        }

        public static bool Foldout(bool expanded, string label) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.Foldout(expanded, label, true);
            }
#endif
            string arrow = expanded ? "\u25BC" : "\u25B6";
            if (GUILayout.Button($"{arrow} {label}", LabelStyle)) {
                expanded = !expanded;
            }
            return expanded;
        }

        public static Enum EnumField(string label, Enum value, bool asFlag) {
            //Get the enum possible values
            var enumType = value.GetType();
            var enumValues = Enum.GetValues(enumType);
            var intValue = Convert.ToInt32(value);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            var enumIterator = 0;

            GUILayout.BeginVertical();
            for (var y = 0; y < 4 && enumIterator < enumValues.Length; y++) {
                GUILayout.BeginHorizontal();
                for (var x = 0; x < 4 && enumIterator < enumValues.Length; x++) {
                    var enumValue = (Enum)enumValues.GetValue(enumIterator);
                    var enumIntValue = Convert.ToInt32(enumValue);
                    bool isSet;
                    if (asFlag) {
                        isSet = (intValue & enumIntValue) == enumIntValue;
                    } else {
                        isSet = intValue == enumIntValue;
                    }
                    var oldColor = GUI.color;
                    GUI.color = isSet ? Color.green : Color.white;
                    if (GUILayout.Button(enumValue.ToString())) {
                        if(isSet) {
                            if (asFlag) {
                                intValue &= ~enumIntValue;
                            } else {
                                intValue = 0;
                            }
                        } else {
                            if (asFlag) {
                                intValue |= enumIntValue;
                            } else {
                                intValue = enumIntValue;
                            }
                        }
                    }
                    GUI.color = oldColor;
                    enumIterator++;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            return intValue.ToEnum(enumType);
        }

        public static int PagedList<T>(IList<T> list, Action<int, T> drawFunction, int page, int pageSize = 30) {
            var pages = Mathf.CeilToInt(list.Count / (float)pageSize)-1;
            
            BeginHorizontal();
            GUILayout.Label("Page: ");
            if (GUILayout.Button("<<<", GUILayout.ExpandWidth(false))) {
                page = 0;
            }
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) {
                page = Math.Max(0, --page);
            }
            page = IntField("", page);
            GUILayout.Label($"/{pages}");
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) {
                page = Math.Min(pages, ++page);
            }
            if (GUILayout.Button(">>>", GUILayout.ExpandWidth(false))) {
                page = pages;
            }
            EndHorizontal();

            var startIndex = page * pageSize;
            var endIndex = Mathf.Min(list.Count, (page + 1) * pageSize);

            for (var i = startIndex; i < endIndex; i++) {
                var element = list[i];
                drawFunction(i, element);
            }
            
            return page;
        }
        
        public static int PagedList<T>(ICollection<T> collection, Action<int, T> drawFunction, int page, int pageSize = 30) {
            var pages = Mathf.CeilToInt(collection.Count / (float)pageSize)-1;
            
            BeginHorizontal();
            GUILayout.Label("Page: ");
            if (GUILayout.Button("<<<", GUILayout.ExpandWidth(false))) {
                page = 0;
            }
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) {
                page = Math.Max(0, --page);
            }
            page = IntField("", page, false);
            GUILayout.Label($"/{pages}");
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) {
                page = Math.Min(pages, ++page);
            }
            if (GUILayout.Button(">>>", GUILayout.ExpandWidth(false))) {
                page = pages;
            }
            EndHorizontal();

            var startIndex = page * pageSize;
            var endIndex = Mathf.Min(collection.Count, (page + 1) * pageSize);
            var count = endIndex - startIndex;
            
            var i = startIndex;
            collection.Skip(startIndex).Take(count).ForEach(e => drawFunction(i++, e));
            return page;
        }

        public static void PagedList<T>(in UnsafeList<T> list, Action<int, T> drawFunction, ref int page, int pageSize = 30) where T : unmanaged {
            var pages = Mathf.CeilToInt(list.Length / (float)pageSize)-1;

            BeginHorizontal();
            GUILayout.Label("Page: ");
            if (GUILayout.Button("<<<", GUILayout.ExpandWidth(false))) {
                page = 0;
            }
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) {
                page = Math.Max(0, --page);
            }
            page = IntField("", page);
            GUILayout.Label($"/{pages}");
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) {
                page = Math.Min(pages, ++page);
            }
            if (GUILayout.Button(">>>", GUILayout.ExpandWidth(false))) {
                page = pages;
            }
            EndHorizontal();

            var startIndex = page * pageSize;
            var endIndex = Mathf.Min(list.Length, (page + 1) * pageSize);

            for (var i = startIndex; i < endIndex; i++) {
                var element = list[i];
                drawFunction(i, element);
            }
        }

        public static void PagedList<T>(NativeArray<T> list, Action<int, T> drawFunction, ref int page, int pageSize = 30) where T : unmanaged {
            var pages = Mathf.CeilToInt(list.Length / (float)pageSize)-1;

            BeginHorizontal();
            GUILayout.Label("Page: ");
            if (GUILayout.Button("<<<", GUILayout.ExpandWidth(false))) {
                page = 0;
            }
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) {
                page = Math.Max(0, --page);
            }
            page = IntField("", page);
            GUILayout.Label($"/{pages}");
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) {
                page = Math.Min(pages, ++page);
            }
            if (GUILayout.Button(">>>", GUILayout.ExpandWidth(false))) {
                page = pages;
            }
            EndHorizontal();

            var startIndex = page * pageSize;
            var endIndex = Mathf.Min(list.Length, (page + 1) * pageSize);

            for (var i = startIndex; i < endIndex; i++) {
                var element = list[i];
                drawFunction(i, element);
            }
        }

        public static void PagedList<T>(UnsafeArray<T> list, Action<uint, T> drawFunction, ref int page, int pageSize = 30) where T : unmanaged {
            var pages = Mathf.CeilToInt(list.Length / (float)pageSize)-1;

            BeginHorizontal();
            GUILayout.Label("Page: ");
            if (GUILayout.Button("<<<", GUILayout.ExpandWidth(false))) {
                page = 0;
            }
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) {
                page = Math.Max(0, --page);
            }
            page = IntField("", page);
            GUILayout.Label($"/{pages}");
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) {
                page = Math.Min(pages, ++page);
            }
            if (GUILayout.Button(">>>", GUILayout.ExpandWidth(false))) {
                page = pages;
            }
            EndHorizontal();

            var startIndex = (uint)(page * pageSize);
            var endIndex = Mathf.Min(list.Length, (page + 1) * pageSize);

            for (var i = startIndex; i < endIndex; i++) {
                var element = list[i];
                drawFunction(i, element);
            }
        }
        #endregion Drawers
        
        #region Scopes
        public static void BeginHorizontal(params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                EditorGUILayout.BeginHorizontal(options);
                return;
            }
#endif
            GUILayout.BeginHorizontal(options);
        }
        public static void EndHorizontal() {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                EditorGUILayout.EndHorizontal();
                return;
            }
#endif
            GUILayout.EndHorizontal();
        }
        
        public static void BeginVertical(params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                EditorGUILayout.BeginVertical(options);
                return;
            }
#endif
            GUILayout.BeginVertical(options);
        }
        public static void EndVertical() {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                EditorGUILayout.EndVertical();
                return;
            }
#endif
            GUILayout.EndVertical();
        }
        
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal = false, bool alwaysShowVertical = false, params GUILayoutOption[] options) {
#if UNITY_EDITOR
            if (s_guiLayout <= 0) {
                return EditorGUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            }
#endif
            return GUILayout.BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
        }
        public static void EndScrollView() {
#if  UNITY_EDITOR
            if (s_guiLayout <= 0) {
                EditorGUILayout.EndScrollView();
                return;
            }
#endif
            GUILayout.EndScrollView();
        }

        public static void BeginIndent() {
            BeginHorizontal();
            GUILayout.Space(16);
            BeginVertical();
        }

        public static void EndIndent() {
            EndVertical();
            EndHorizontal();
        }
        #endregion Scopes

        public class CheckChangeScope : IDisposable {
            bool _oldValue;

            public CheckChangeScope() {
                _oldValue = GUI.changed;
                GUI.changed = false;
            }

            public static implicit operator bool(CheckChangeScope scope) => GUI.changed;

            public void Dispose() {
                GUI.changed |= _oldValue;
            }
        }
        
        public class IndentScope : IDisposable {
            public IndentScope() {
#if UNITY_EDITOR
                if (s_guiLayout <= 0) {
                    EditorGUI.indentLevel++;
                }
#else
                BeginIndent();
#endif
            }

            public void Dispose() {
#if UNITY_EDITOR
                if (s_guiLayout <= 0) {
                    EditorGUI.indentLevel--;
                }
#else
                EndIndent();
#endif
            }
        }
        
        /// <summary>
        /// Caches the button style of the skin currently active. GUI.skin is different depending on current context
        /// </summary>
        static void CacheStyleWithChanges(ref GUIStyle buttonStyle) {
            if (buttonStyle == null) {
                buttonStyle = new GUIStyle(GUI.skin.button) {
                    alignment = TextAnchor.MiddleLeft,
                    font = TGGUILayout.LabelStyle.font
                };
                buttonStyle.padding.left *= 3;
            }
        }
    }
}