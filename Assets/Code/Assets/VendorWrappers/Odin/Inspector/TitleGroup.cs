using System;

namespace Sirenix.OdinInspector
{
    public class TitleGroupAttribute : Attribute
    {
        public string Subtitle;
        public TitleAlignments Alignment;
        public bool HorizontalLine;
        public bool BoldTitle;
        public bool Indent;

        public TitleGroupAttribute(string title, string subtitle = null, TitleAlignments alignment = TitleAlignments.Left, bool horizontalLine = true, bool boldTitle = true, bool indent = false, float order = 0.0f) { }
    }
}