using System;
using System.Linq;
using UnityEngine;

namespace Vendor.xNode.Scripts.Editor.GenericMenus {
    public class NodeGenericMenuItem {
        const int Width = 230;
        static GUIStyle s_buttonStyle;

        static GUIStyle ButtonStyle {
            get {
                if (s_buttonStyle == null) {
                    s_buttonStyle = new GUIStyle(GUI.skin.button);
                    s_buttonStyle.wordWrap = true;
                }
                return s_buttonStyle;
            }
        }
        
        public readonly GUIContent name;
        public readonly string path;
        readonly GUIContent _fullContent;
        readonly Action _callback;
        readonly bool _enable;
        
        public NodeGenericMenuItem(GUIContent fullContent, Action callback, bool enable) {
            this._fullContent = fullContent;
            this._callback = callback;
            this._enable = enable;
            if (!string.IsNullOrWhiteSpace(fullContent.text) && fullContent.text.Contains("/")) {
                var split = fullContent.text.Split('/');
                name = new GUIContent(split.Last(), _fullContent.text);
                path = string.Join("/", new ArraySegment<string>(split, 0, split.Length - 1));
            } else {
                name = _fullContent;
                path = string.Empty;
            }
        }

        public bool IsValid(string search) {
            return string.IsNullOrWhiteSpace(search) || _fullContent.text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        public bool OnGUI() {
            bool clicked = false;
            var oldEnable = GUI.enabled;
            GUI.enabled = _enable;
            if (GUILayout.Button(name, ButtonStyle, GUILayout.Width(Width))) {
                _callback?.Invoke();
                clicked = true;
            }
            GUI.enabled = oldEnable;
            return clicked;
        }

        public float Height() => ButtonStyle.CalcHeight(name, Width);
    }
}