using System;

namespace QFSW.QC
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public abstract class SuggestorTagAttribute : Attribute
    {
        public abstract IQcSuggestorTag[] GetSuggestorTags();
    }
}