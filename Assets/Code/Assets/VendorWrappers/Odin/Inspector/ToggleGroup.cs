using System;

namespace Sirenix.OdinInspector
{
    public class ToggleGroupAttribute : Attribute
    {
        public string ToggleGroupTitle;
		public bool CollapseOthersOnExpand;

		public ToggleGroupAttribute(string toggleMemberName, float order = 0.0f, string groupTitle = null) { }
		public ToggleGroupAttribute(string toggleMemberName, string groupTitle) { }
		public ToggleGroupAttribute(string toggleMemberName, float order, string groupTitle, string titleStringMemberName) { }
    }
}