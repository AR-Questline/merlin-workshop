using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Attributes {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true), Conditional("UNITY_EDITOR")]
    public class IconRenderingSettingsAttribute : Attribute { }
}