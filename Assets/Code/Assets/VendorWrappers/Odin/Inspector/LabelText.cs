using System;

namespace Sirenix.OdinInspector
{
    public class LabelTextAttribute : Attribute
    {
        public string Text;
        public bool NicifyText;
        public SdfIconType Icon;
        public string IconColor;
    
        public LabelTextAttribute(string text) { }
        public LabelTextAttribute(SdfIconType icon) { }
        public LabelTextAttribute(string text, bool nicifyText) { }
        public LabelTextAttribute(string text, SdfIconType icon) { }
        public LabelTextAttribute(string text, bool nicifyText, SdfIconType icon) { }
    }
}