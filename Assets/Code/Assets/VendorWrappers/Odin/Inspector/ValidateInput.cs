using System;

namespace Sirenix.OdinInspector
{
    public class ValidateInputAttribute : Attribute
    {
        public ValidateInputAttribute(string method, string text = null) { }
        
    }
}