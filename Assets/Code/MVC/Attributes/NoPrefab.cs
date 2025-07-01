using System;

namespace Awaken.TG.MVC.Attributes
{
    /// <summary>
    /// Attribute for use with View classes - specifies that given view doesn't spawn prefab from Resources,
    /// instead it's added on to empty GameObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NoPrefab : Attribute {
    }
}