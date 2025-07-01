using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
    public class DisplayAsStringAttribute : Attribute
    {
        public bool Overflow;
        public TextAlignment Alignment;
        public int FontSize;
        public bool EnableRichText;
        public string Format;
        
        public DisplayAsStringAttribute() { }
        public DisplayAsStringAttribute(bool overflow) { }
        public DisplayAsStringAttribute(TextAlignment alignment) { }
        public DisplayAsStringAttribute(int fontSize) { }
        public DisplayAsStringAttribute(bool overflow, TextAlignment alignment) { }
        public DisplayAsStringAttribute(bool overflow, int fontSize) { }
        public DisplayAsStringAttribute(int fontSize, TextAlignment alignment) { }
        public DisplayAsStringAttribute(bool overflow, int fontSize, TextAlignment alignment) { }
        public DisplayAsStringAttribute(TextAlignment alignment, bool enableRichText) { }
        public DisplayAsStringAttribute(int fontSize, bool enableRichText) { }
        public DisplayAsStringAttribute(bool overflow, TextAlignment alignment, bool enableRichText) { }
        public DisplayAsStringAttribute(bool overflow, int fontSize, bool enableRichText) { }
        public DisplayAsStringAttribute(int fontSize, TextAlignment alignment, bool enableRichText) { }
        public DisplayAsStringAttribute(bool overflow, int fontSize, TextAlignment alignment, bool enableRichText) { }
    }
}