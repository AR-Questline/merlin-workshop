using System;
using System.Collections.Generic;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TabGroupAttribute : GroupAttribute
    {
        public string TabName;
        public string TabId;
        public bool UseFixedHeight;
        public bool Paddingless;
        public bool HideTabGroupIfTabGroupOnlyHasOneTab;
        public string TextColor;
        public SdfIconType Icon;
        public TabLayouting TabLayouting;
        public List<TabGroupAttribute> Tabs;

        public TabGroupAttribute(string tab, bool useFixedHeight = false, float order = 0.0f) { }
        public TabGroupAttribute(string group, string tab, bool useFixedHeight = false, float order = 0.0f) { }

        public TabGroupAttribute(string group, string tab, SdfIconType icon, bool useFixedHeight = false, float order = 0.0f) { }
    }

    public class TabSubGroupAttribute : GroupAttribute
    {
        public TabGroupAttribute Tab;

        public TabSubGroupAttribute(TabGroupAttribute tab, string groupId, float order) { }
    }
    
    public enum TabLayouting
    {
        MultiRow,
        Shrink,
    }
}