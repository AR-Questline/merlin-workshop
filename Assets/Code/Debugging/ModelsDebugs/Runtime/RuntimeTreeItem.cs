using System;
using System.Collections.Generic;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Runtime {
    public class RuntimeTreeItem {
        public string displayName;
        public bool isRoot;

        public MembersList MembersList { get; }

        bool _expanded;
        bool _hasChildren;

        RuntimeTreeItem _parent;
        List<RuntimeTreeItem> _children = new List<RuntimeTreeItem>();

        RuntimeTreeView ParentTree { get; }
        bool IsSelected => MembersList != null && ParentTree.SelectedMember == MembersList;

        // === Creation
        public RuntimeTreeItem(RuntimeTreeView tree) {
            ParentTree = tree;
        }

        public RuntimeTreeItem(RuntimeTreeView tree, MembersList membersList) {
            ParentTree = tree;
            MembersList = membersList;
            displayName = MembersList.Name;
        }

        public void AddChild(RuntimeTreeItem child) {
            _children.Add(child);
            child._parent = this;
            _hasChildren = true;
        }

        // === Drawing
        public void Draw(string search) {
            if (isRoot) {
                DoDrawChildren(search);
                return;
            }
            if (string.IsNullOrWhiteSpace(search)) {
                if (_hasChildren) {
                    TGGUILayout.BeginHorizontal();
                    string arrow = _expanded ? "\u25BC" : "\u25B6";
                    if (GUILayout.Button(arrow, TGGUILayout.LabelStyle, GUILayout.ExpandWidth(false))) {
                        _expanded = !_expanded;
                    }
                }
                    
                DoDraw();
                    
                if (_hasChildren) {
                    TGGUILayout.EndHorizontal();
                    
                    if (_expanded) {
                        TGGUILayout.BeginHorizontal();
                        GUILayout.Space(25);
                        DoDrawChildren(search);
                        TGGUILayout.EndHorizontal();
                    }
                }
            } else {
                if (ParentTree.SearchDisplayedItems >= RuntimeTreeView.MaxSearchItems) {
                    return;
                }
                if (displayName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) >= 0) {
                    ParentTree.SearchDisplayedItems++;
                    DoDraw();
                }
                DoDrawChildren(search);
            }
        }

        void DoDraw() {
            var oldColor = GUI.backgroundColor;
            if (IsSelected) {
                GUI.backgroundColor = Color.blue;
                GUILayout.BeginVertical(TGGUILayout.WhiteBackgroundStyle);
            }

            int count = _children.Count;
            string buttonName = count > 0 ? $"{displayName} ({count})" : displayName;
            if (GUILayout.Button(buttonName, TGGUILayout.LabelStyle)) {
                ParentTree.SelectedMember = MembersList;
            }

            if (MembersList != null) {
                MembersList.ScrollY = GUILayoutUtility.GetLastRect().y;
            }
            
            if (IsSelected) {
                GUILayout.EndVertical();
            }
            GUI.backgroundColor = oldColor;
        }

        void DoDrawChildren(string search) {
            TGGUILayout.BeginVertical();
            foreach (var child in _children) {
                child.Draw(search);
            }
            TGGUILayout.EndVertical();
        }

        // === Operations
        public void FocusSelected() {
            if (ParentTree.SelectedMember == null) {
                return;
            }

            if (IsSelected && _parent != null) {
                ExpandParent();
            } else {
                foreach (var child in _children) {
                    child.FocusSelected();
                }
            }
        }

        void ExpandParent() {
            if (_parent != null) {
                _parent._expanded = true;
                _parent.ExpandParent();
            }
        }
    }
}