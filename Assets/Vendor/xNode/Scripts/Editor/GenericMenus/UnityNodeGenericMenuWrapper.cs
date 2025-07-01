using System;
using UnityEditor;
using UnityEngine;

namespace Vendor.xNode.Scripts.Editor.GenericMenus {
    public class UnityNodeGenericMenuWrapper : INodeGenericMenu {
        GenericMenu _menu = new GenericMenu();
        public void DropDown(Rect position) {
            _menu.DropDown(position);
        }

        public int GetItemCount() {
            return _menu.GetItemCount();
        }

        public void AddItem(GUIContent content, bool @on, Action callback) {
            _menu.AddItem(content, on, () => callback());
        }

        public void AddSeparator(string path) {
            _menu.AddSeparator(path);
        }

        public void AddDisabledItem(GUIContent content) {
            _menu.AddDisabledItem(content);
        }
    }
}