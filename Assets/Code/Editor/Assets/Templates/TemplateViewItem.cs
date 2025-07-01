using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Awaken.TG.Editor.Assets.Templates {
    public class TemplateViewItem : TreeViewItem {
        const string BaseTarget = "Data/Templates";

        public static readonly TemplateViewItem EmptyItem = new TemplateViewItem(-2, string.Empty, string.Empty);
        
        public string Target { get; set; }
        public string Guid { get; }

        public bool IsFile => Guid != null;
        
        bool _enabled;

        public bool Enabled {
            get => _enabled;
            set {
                if (value != _enabled) {
                    _enabled = value;
                    if (hasChildren) {
                        foreach (TemplateViewItem child in children.Cast<TemplateViewItem>()) {
                            child.Enabled = value;
                        }
                    }
                }
            }
        }
        
        public TemplateViewItem(int id, string name, string guid) : base(id) {
            base.displayName = name;
            Target = name == "Resources" ? BaseTarget : name;
            Guid = guid;
        }

        public string GetPath() {
            if (parent is TemplateViewItem templateItem) {
                string parentsPath = templateItem.GetPath();
                if (!parentsPath.EndsWith('/')) {
                    parentsPath += '/';
                }

                return parentsPath + Target;
            }
            return Target;
        }

        public void GetEnabledItemsRecursively(List<TemplateViewItem> result) {
            if (hasChildren) {
                foreach (TemplateViewItem child in children.Cast<TemplateViewItem>()) {
                    child.GetEnabledItemsRecursively(result);
                }
            } else if (Enabled) {
                result.Add(this);
            }
        }

        public void Sort() {
            if (hasChildren) {
                foreach (TemplateViewItem child in children.Cast<TemplateViewItem>()) {
                    child.Sort();
                }

                children = children.OrderBy(c => c is TemplateViewItem t ? (t.IsFile ? 1 : 0) : 0)
                    .ThenBy(c => c.displayName)
                    .ToList();
            }
        }
    }
}