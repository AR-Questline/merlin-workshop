using System;

namespace Sirenix.OdinInspector
{
    public class HorizontalGroupAttribute : GroupAttribute
    {
        public float Width;
        public float MarginLeft;
        public float MarginRight;
        public float PaddingLeft;
        public float PaddingRight;
        public float MinWidth;
        public float MaxWidth;
        public float Gap = 3f;
        public string Title;
        public bool DisableAutomaticLabelWidth;
        public float LabelWidth;
        
        public HorizontalGroupAttribute(string group, float width = 0.0f, int marginLeft = 0, int marginRight = 0, float order = 0.0f) { }
        public HorizontalGroupAttribute(float width = 0.0f, int marginLeft = 0, int marginRight = 0, float order = 0.0f) { }
    }
}