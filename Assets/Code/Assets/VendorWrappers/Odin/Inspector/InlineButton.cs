using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class InlineButtonAttribute : Attribute
    {
        public string Action;
        public string Label;
        public string ShowIf;
        public string ButtonColor;
        public string TextColor;
        public SdfIconType Icon;
        public IconAlignment IconAlignment;
        public string MemberMethod { get => default; set { } }

        public InlineButtonAttribute(string action, string label = null) { }
        public InlineButtonAttribute(string action, SdfIconType icon, string label = null) { }
    }
}