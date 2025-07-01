using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class HideIfGroupAttribute : GroupAttribute
    {
        public object Value;

        public bool Animate
        {
            get => this.AnimateVisibility;
            set => this.AnimateVisibility = value;
        }

        public string MemberName
        {
            get => this.Condition;
            set => this.Condition = value;
        }

        public string Condition
        {
            get => !string.IsNullOrEmpty(this.VisibleIf) ? this.VisibleIf : this.GroupName;
            set => this.VisibleIf = value;
        }

        public HideIfGroupAttribute(string path, bool animate = true) { }
        public HideIfGroupAttribute(string path, object value, bool animate = true) { }
    }
}