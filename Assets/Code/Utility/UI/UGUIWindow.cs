using System;
using UnityEngine;

namespace Awaken.Utility.UI {
    public class UGUIWindow {
        static int s_nextId;

        Rect _position;
        readonly int _id;
        readonly string _title;
        readonly Action _drawCallback;
        readonly Action _closeCallback;
        readonly Action _drawToolbarLeft;
        readonly Action _drawToolbarRight;

        // Resize
        bool _hasMouseDown;
        Vector2 _downMousePosition;
        Vector2 _downMouseSize;

        public Rect Position => _position;

        public UGUIWindow(Rect position, string title, Action drawCallback, Action closeCallback, Action drawToolbarLeft = null, Action drawToolbarRight = null) {
            _id = s_nextId++;
            _position = position;
            _title = title;
            _drawCallback = drawCallback;
            _closeCallback = closeCallback;
            _drawToolbarLeft = drawToolbarLeft;
            _drawToolbarRight = drawToolbarRight;
        }

        public void OnGUI() {
            _position = Resize(_position);
            _position = GUILayout.Window(_id, _position, DrawWindow, _title);
        }

        void DrawWindow(int id) {
            if (id != _id) {
                return;
            }
            
            GUI.DragWindow(new Rect(0, 0, Screen.width+5, GUI.skin.window.lineHeight));

            GUILayout.BeginHorizontal();
            _drawToolbarLeft?.Invoke();
            GUILayout.FlexibleSpace();
            _drawToolbarRight?.Invoke();
            if (GUILayout.Button("x", GUILayout.Width(40))){
                _closeCallback();
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            
            _drawCallback();
        }
        
        Rect Resize(Rect window, float detectionRange = 16f) {
            Rect handle = window;
 
            handle.xMin = handle.xMax - detectionRange;
            handle.xMax += 4;

            handle.yMin = handle.yMax - detectionRange;
            handle.yMax += 4;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.gray;
            GUI.Box(handle, "", TGGUILayout.WhiteBackgroundStyle);
            GUI.backgroundColor = oldColor;

            Event current = Event.current;

            if (current.type == EventType.MouseDown) {
                _hasMouseDown = handle.Contains(current.mousePosition) && current.button == 0;
                _downMousePosition = current.mousePosition;
                _downMouseSize = new(window.width, window.height);
            } else if (current.type == EventType.MouseUp) {
                _hasMouseDown = false;
            }
            
            if (_hasMouseDown && current.type == EventType.MouseDrag) {
                var mouseAbsoluteDelta = current.mousePosition - _downMousePosition;
                window.width = _downMouseSize.x + mouseAbsoluteDelta.x;
                window.height = _downMouseSize.y + mouseAbsoluteDelta.y;
            }
            
            return window;
        }
    }
}