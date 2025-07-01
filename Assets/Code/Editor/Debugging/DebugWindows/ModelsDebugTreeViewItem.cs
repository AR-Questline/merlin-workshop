using Awaken.TG.Debugging.ModelsDebugs;
using UnityEditor.IMGUI.Controls;

namespace Awaken.TG.Editor.Debugging.DebugWindows {
    public class ModelsDebugTreeViewItem : TreeViewItem {
        public MembersList MembersList { get; }
        public override string displayName {
            get => Count > 0 ? $"{MembersList.Name} ({Count})" : MembersList.Name;
            set{}
        }

        public int Count { get; set; }

        public ModelsDebugTreeViewItem(int id, MembersList membersList) : base(id) {
            MembersList = membersList;
        }
    }
}