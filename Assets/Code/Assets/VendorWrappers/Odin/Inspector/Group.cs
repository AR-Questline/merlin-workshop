using System;

namespace Sirenix.OdinInspector
{
    public abstract class GroupAttribute : Attribute
    {
        public string GroupID;
        public string GroupName;
        public float Order;
        public bool HideWhenChildrenAreInvisible = true;
        public string VisibleIf;
        public bool AnimateVisibility = true;
    }
}