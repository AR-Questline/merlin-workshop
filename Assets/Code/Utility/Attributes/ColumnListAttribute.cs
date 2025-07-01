using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Attributes {
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class ColumnListAttribute : Attribute { }
}