using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Templates;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates {
    public class TemplatesTreeView : TreeView {
        const float ToggleWidth = 18f;

        readonly GUIContent _goIcon= EditorGUIUtility.IconContent("GameObject Icon");
        readonly List<string> _guids;

        public TemplateViewItem Root { get; private set; }

        public TemplatesTreeView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader, List<string> guids)
            : base(treeViewState, multiColumnHeader) {
            extraSpaceBeforeIconAndLabel = ToggleWidth;
            _guids = guids;
            Reload();
        }

        protected override TreeViewItem BuildRoot () {
            Root = new TemplateViewItem(0, "Assets", null) {
                depth = -1
            };

            int id = 0;
            foreach (string guid in _guids) {
                ProcessGuid(guid, ref id);
            }

            if (!Root.hasChildren) {
                Root.AddChild(TemplateViewItem.EmptyItem);
            }

            SetupDepthsFromParentsAndChildren (Root);
            Root.Sort();
            
            return Root;
        }

        void ProcessGuid(string guid, ref int id) {
            if (AddressableHelper.IsAddressable(guid)) {
                return;
            }
            string paths = AssetDatabase.GUIDToAssetPath(guid);
            string[] directories = paths.Split('/');

            AddChild(Root, guid, directories, 1, ref id);
        }

        void AddChild(TreeViewItem treeItem, string guid, string[] pathSplitted, int current, ref int id) {
            if (current >= pathSplitted.Length) {
                return;
            }
            
            if (treeItem.hasChildren) {
                foreach (TreeViewItem child in treeItem.children) {
                    if (child.displayName == pathSplitted[current]) {
                        AddChild(child, guid, pathSplitted, ++current, ref id);
                        return;
                    }
                }
            }

            var newItem = new TemplateViewItem(++id, pathSplitted[current], current == pathSplitted.Length - 1 ? guid : null);
            treeItem.AddChild(newItem);
            AddChild(newItem, guid, pathSplitted, ++current, ref id);
        }

        protected override void RowGUI(RowGUIArgs args) {

            for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }
        
        void CellGUI (Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args) {
            if (item is TemplateViewItem templateItem) {
                CenterRectUsingSingleLineHeight(ref cellRect);

                switch (column) {
                    case 0:
                        DrawItem(cellRect, item, ref args, templateItem);
                        break;
                    case 1:
                        DrawPingButton(cellRect, templateItem);
                        break;
                    default: {
                        DrawTarget(cellRect, templateItem);
                        break;
                    }
                }
            }
        }

        static void DrawTarget(Rect cellRect, TemplateViewItem templateItem) {
            if (templateItem.IsFile) {
                GUI.TextField(cellRect, templateItem.GetPath());
            } else {
                templateItem.Target = GUI.TextField(cellRect, templateItem.Target);
            }
        }

        void DrawPingButton(Rect cellRect, TemplateViewItem templateItem) {
            if (templateItem.IsFile && GUI.Button(cellRect, _goIcon)) {
                EditorGUIUtility.PingObject(AssetsUtils.LoadAssetByGuid<Object>(templateItem.Guid));
            }
        }

        void DrawItem(Rect cellRect, TreeViewItem item, ref RowGUIArgs args, TemplateViewItem templateItem) {
            Rect toggleRect = cellRect;
            toggleRect.x += GetContentIndent(item);
            toggleRect.width = ToggleWidth;
            if (toggleRect.xMax < cellRect.xMax) {
                templateItem.Enabled = EditorGUI.Toggle(toggleRect, templateItem.Enabled);
            }

            args.rowRect = cellRect;
            base.RowGUI(args);
        }

        public bool IsEmpty() {
            return !Root.hasChildren || Root.children[0] == TemplateViewItem.EmptyItem;
        }
    }
}