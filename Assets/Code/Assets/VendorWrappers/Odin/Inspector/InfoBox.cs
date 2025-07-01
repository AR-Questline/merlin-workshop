using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class InfoBoxAttribute : Attribute
    {
        public string VisibleIf;
        public string Message { get; }
        public InfoMessageType InfoMessageType { get; }
        public SdfIconType Icon { get; }
        public bool GUIAlwaysEnabled;
        
        
        public InfoBoxAttribute(string message, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIfMemberName = null) { }
        public InfoBoxAttribute(string message, string visibleIfMemberName) { }
        public InfoBoxAttribute(string message, SdfIconType icon, string visibleIfMemberName = null) { }
    }
    
    public enum InfoMessageType
    {
        None,
        Info,
        Warning,
        Error,
    }
}