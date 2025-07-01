using System;
using UnityEngine;

namespace Vendor.xNode.Scripts.Editor.GenericMenus {
    public interface INodeGenericMenu {
        void DropDown(Rect position);
        int GetItemCount();
        void AddItem(GUIContent content, bool on, Action callback);
        void AddSeparator(string path);
        void AddDisabledItem(GUIContent content);
    }
}