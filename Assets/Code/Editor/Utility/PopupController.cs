using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public class PopupController<T> {
        // === Cache and state
        ICollection<T> _backingElements;
        string[] _options;
        int _selectedIndex;
        bool _withNone;

        // === Properties
        public T Selected {
            get {
                if (_withNone) {
                    return _selectedIndex == 0 ?  default(T) : _backingElements.ElementAt(_selectedIndex - 1);
                }
                return _backingElements.ElementAt(_selectedIndex);
            }
        }
        
        // === Constructor
        public PopupController(ICollection<T> elements, T selected = default(T), bool withNone = true, Func<T, string> nameExtractor = null) {

            if (nameExtractor == null) {
                nameExtractor = NameExtractor();
            }
            
            _backingElements = elements.OrderBy( e => nameExtractor(e) ).ToList();
            _withNone = withNone;

            int offset = _withNone ? 1 : 0;
            _options = new string[_backingElements.Count + offset];
            if (withNone) {
                _options[0] = "[NONE]";
            }
            
            for (int i = 0; i < _backingElements.Count; i++) {
                var element = _backingElements.ElementAt(i);
                _options[i + offset] = nameExtractor(element);
            }

            _selectedIndex = _backingElements.IndexOf(selected);
            if (_selectedIndex == -1) {
                _selectedIndex = 0;
            }
        }

        // === Drawing
        public void DrawPopup(string label, Action<T> onChange = null) {
            EditorGUI.BeginChangeCheck();
            _selectedIndex = EditorGUILayout.Popup(label, _selectedIndex, _options);
            if (EditorGUI.EndChangeCheck()) {
                onChange?.Invoke(Selected);
            }
        }
        
        // === Operators
        public static explicit operator T(PopupController<T> controller) => controller.Selected;

        // === Utils
        static Func<T, string> NameExtractor() {
            return element => {
                if ((element as object) is Object unityObject) {
                    return unityObject.name;
                }
                return element.ToString();
            };
        }
    }
}