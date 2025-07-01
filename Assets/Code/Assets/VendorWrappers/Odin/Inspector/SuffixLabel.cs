using System;

namespace Sirenix.OdinInspector
{
    public class SuffixLabelAttribute : Attribute
    {
        public string Label;
        public bool Overlay;
        public string IconColor;
        public SdfIconType Icon;
        
        public SuffixLabelAttribute(string label, bool overlay = false) { }
        public SuffixLabelAttribute(string label, SdfIconType icon, bool overlay = false) { }
        public SuffixLabelAttribute(SdfIconType icon) { }
    }
}