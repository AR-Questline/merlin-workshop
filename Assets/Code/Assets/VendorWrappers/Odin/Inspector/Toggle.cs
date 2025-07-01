using System;

namespace Sirenix.OdinInspector
{
    public class ToggleAttribute : Attribute
    {
        public string ToggleMemberName;
        public bool CollapseOthersOnExpand;

        public ToggleAttribute(string toggleMemberName) { }
    }
}