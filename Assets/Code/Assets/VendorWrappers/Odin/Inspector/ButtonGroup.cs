namespace Sirenix.OdinInspector
{
    public class ButtonGroupAttribute : GroupAttribute
    {
        public int ButtonHeight;
        public IconAlignment IconAlignment;
        public int ButtonAlignment;
        public bool Stretch;
        
        public ButtonGroupAttribute(string group = "_DefaultGroup", float order = 0.0f) { }
    }
}