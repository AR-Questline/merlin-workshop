using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vendor.xNode.Scripts.Editor.GenericMenus {
    public class NodeGenericMenuPopup : EditorWindow {
        NodeGenericMenu _generic;
        SearchField _searchField;
        string _searchText = "";
        Vector2 _scroll = Vector2.zero;
        NodeGenericMenuItem[] _filtered;

        public static void Show(Rect displayPosition, NodeGenericMenu menu) {
            var window = CreateInstance<NodeGenericMenuPopup>();
            window._generic = menu;
            window._searchField = new SearchField();
            window._searchField.SetFocus();
            window.position = new Rect(displayPosition.position, new Vector2(250, 500));

            window._filtered = menu.items.ToArray();
            window.Resize();
            window.ShowPopup();
            window.Focus();
        }

        void OnGUI() {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
                Close();
                return;
            }
            
            var searchBoxRect = GUILayoutUtility.GetRect(position.width-50, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            _searchText = _searchField.OnGUI( searchBoxRect, _searchText );
            if (EditorGUI.EndChangeCheck()) {
                _filtered = _generic.items.Where(i => i.IsValid(_searchText)).ToArray();
                Resize();
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            bool shouldClose = false;
            foreach (var item in _filtered) {
                shouldClose = item.OnGUI() || shouldClose;
            }
            GUILayout.EndScrollView();

            if (shouldClose) {
                Close();
            }
        }

        void OnLostFocus() {
            Close();
        }

        void Resize() {
            var pos = position;
            var itemsHeight = EditorGUIUtility.singleLineHeight * 0.7f + _filtered.Sum( i => i.Height());
            var reservedSpace = 2 * EditorGUIUtility.singleLineHeight;
            pos.height = Mathf.Clamp(reservedSpace + itemsHeight, reservedSpace, 500);
            position = pos;
        }
    }
}