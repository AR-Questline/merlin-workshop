using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Utility.Attributes {
    [Conditional("UNITY_EDITOR")]
    public class RichEnumLabelAttribute : Attribute {}
}