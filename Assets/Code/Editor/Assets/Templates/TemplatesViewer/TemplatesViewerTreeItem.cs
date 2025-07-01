using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    public class TemplatesViewerTreeItem : TreeViewItem {
        public ITemplate Template { get; }
        public Object TemplateObject { get; }
        public SerializedObject SerializedObject { get; }
        
        public TemplatesViewerTreeItem(int id, int depth, ITemplate template) : base(id, depth, template.ToString()) {
            Template = template;
            TemplateObject = template as Object;
            SerializedObject = new SerializedObject(TemplateObject);
        }
    }
}