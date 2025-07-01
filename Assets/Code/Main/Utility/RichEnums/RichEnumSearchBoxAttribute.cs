using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.RichEnums {
    /// <summary>
    /// If you can not use <see cref="RichEnumExtendsAttribute"/> this will give you search box to easier assigning ability
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RichEnumSearchBoxAttribute : PropertyAttribute {
    }
}