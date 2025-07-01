using System;

namespace Sirenix.OdinInspector
{
    public class TypeFilterAttribute : Attribute
    {
        public string FilterGetter;
        public string DropdownTitle;
        public bool DrawValueNormally;
        public string MemberName
        {
            get => this.FilterGetter;
            set => this.FilterGetter = value;
        }

        public TypeFilterAttribute(string filterGetter) { }
    }
}