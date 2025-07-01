using UnityEditor.IMGUI.Controls;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns {
    public class DraggableMultiColumnHeader : MultiColumnHeader {

        public DraggableMultiColumnHeader(MultiColumnHeaderState state) : base(state) {
            allowDraggingColumnsToReorder = true;
        }
    }
}