using System;

namespace Sirenix.OdinInspector
{
    public class RequiredAttribute : Attribute
    {
        public RequiredAttribute() { }
        public RequiredAttribute(string errorMessage, InfoMessageType messageType) { }
        public RequiredAttribute(string errorMessage) { }
        public RequiredAttribute(InfoMessageType messageType) { }
    }
}