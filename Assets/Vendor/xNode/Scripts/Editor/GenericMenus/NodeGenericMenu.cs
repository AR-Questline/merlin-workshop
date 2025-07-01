using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vendor.xNode.Scripts.Editor.GenericMenus {
    public class NodeGenericMenu : INodeGenericMenu {
        public List<NodeGenericMenuItem> items = new List<NodeGenericMenuItem>();
        
        public int GetItemCount() => items.Count;

        public void DropDown(Rect position) {
            items.Sort((a, b) => string.Compare(a.path + "/" + a.name.text, b.path + "/" + b.name.text, StringComparison.InvariantCultureIgnoreCase));
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = screenPoint.x;
            position.y = screenPoint.y;
            NodeGenericMenuPopup.Show(position, this);
        }

        public void AddItem(GUIContent content, bool on, Action callback) {
            items.Add(new NodeGenericMenuItem(content, callback, true));
        }

        public void AddSeparator(string path) {
            items.Add(new NodeGenericMenuItem(new GUIContent(path), null, false));
        }

        public void AddDisabledItem(GUIContent content) {
            items.Add(new NodeGenericMenuItem(content, null, false));
        }
    }
}