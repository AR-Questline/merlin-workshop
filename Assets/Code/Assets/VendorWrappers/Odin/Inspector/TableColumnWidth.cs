using System;

namespace Sirenix.OdinInspector
{
    public class TableColumnWidthAttribute : Attribute
    {
        public bool Resizable = true;
        public TableColumnWidthAttribute(int width, bool resizable = true) { }
    }
}