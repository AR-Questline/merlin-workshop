using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Attributes.Tags {
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class TagsAttribute : Attribute {
        public TagsCategory tagsCategory;

        public TagsAttribute(TagsCategory tagsCategory) {
            this.tagsCategory = tagsCategory;
        }
    }
}