namespace Sirenix.OdinInspector
{
    public class ShowIfGroupAttribute : GroupAttribute
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

        public ShowIfGroupAttribute(string path, bool animate = true) { }
        public ShowIfGroupAttribute(string path, object value, bool animate = true) { }
    }
}