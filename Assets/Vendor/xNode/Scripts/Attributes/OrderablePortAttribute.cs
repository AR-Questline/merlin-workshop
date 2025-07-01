using System;
using System.Diagnostics;

namespace XNode.Attributes {
    [Conditional("UNITY_EDITOR")]
    public class OrderablePortAttribute : Attribute {}
}