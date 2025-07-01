using System;
using System.Diagnostics;

namespace XNode.Attributes {
    [Conditional("UNITY_EDITOR")]
    public class HelpButtonAttribute : Attribute {
        public const string CommonPrefix = "https://www.notion.so/awaken/Skill-graph-y-";
        
        public string url;
        public HelpButtonAttribute(string url, bool useCommonPrefix = true) {
            if (useCommonPrefix) {
                this.url = CommonPrefix + url;
            } else {
                this.url = url;
            }
        }
    }
}