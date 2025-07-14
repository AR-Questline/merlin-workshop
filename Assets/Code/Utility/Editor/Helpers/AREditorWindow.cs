using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Helpers {
    public abstract class AREditorWindow : EditorWindow {
        protected SerializedObject _windowObject;
        protected List<ButtonDefinition> _buttons = new List<ButtonDefinition>();
        protected Dictionary<string, Action<SerializedProperty>> _customDrawers = new Dictionary<string, Action<SerializedProperty>>();

        Vector2 _scrollPosition;

        protected virtual void OnEnable() {
            _windowObject = new SerializedObject(this);
        }

        protected virtual void OnDisable() {
            _windowObject?.Dispose();
            _windowObject = null;
            _buttons.Clear();
        }

        protected virtual void OnGUI() {
            _windowObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            var propertiesIterator = _windowObject.GetIterator();
            for (var enterChildren = true; propertiesIterator.NextVisible(enterChildren); enterChildren = false) {
                if (propertiesIterator.name == "m_Script") {
                    continue;
                }
                if (_customDrawers.TryGetValue(propertiesIterator.name, out var customDrawer)) {
                    customDrawer(propertiesIterator);
                } else {
                    EditorGUILayout.PropertyField(propertiesIterator, true);
                }
            }
            EditorGUILayout.EndScrollView();

            _windowObject.ApplyModifiedProperties();

            foreach (var button in _buttons) {
                if (button.visible()) {
                    if (GUILayout.Button(button.label)) {
                        button.action?.Invoke();
                    }
                }
            }
        }

        protected void AddButton(string label, Action action, Func<bool> visible = null) {
            _buttons.Add(new ButtonDefinition(label, action, visible));
        }

        protected void AddCustomDrawer(string fieldName, Action<SerializedProperty> drawer) {
            _customDrawers.Add(fieldName, drawer);
        }

        protected readonly struct ButtonDefinition {
            public readonly string label;
            public readonly Action action;
            public readonly Func<bool> visible;

            public ButtonDefinition(string label, Action action, Func<bool> visible = null) {
                this.label = label;
                this.action = action;
                this.visible = visible ?? (() => true);
            }
        }
    }
}