using System;

namespace Vendor.xNode.Scripts.Attributes {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideInStoryGraphAttribute : Attribute { }
}