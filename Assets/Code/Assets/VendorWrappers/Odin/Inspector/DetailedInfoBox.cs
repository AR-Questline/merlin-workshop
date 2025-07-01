using System;

namespace Sirenix.OdinInspector
{
    public class DetailedInfoBoxAttribute : Attribute
    {
        public string Message;
        public string Details;
        public InfoMessageType InfoMessageType;
        public string VisibleIf;

        public DetailedInfoBoxAttribute(string message, string details, InfoMessageType infoMessageType = InfoMessageType.Info, string visibleIf = null) { }
    }
}