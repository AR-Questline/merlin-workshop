using System;

namespace Sirenix.OdinInspector
{
    public class PropertyTooltipAttribute : Attribute
    {
        public string Tooltip;
        public PropertyTooltipAttribute(string tooltip) { }
    }
}