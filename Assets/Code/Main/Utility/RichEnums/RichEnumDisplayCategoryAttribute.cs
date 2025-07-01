using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.RichEnums {
    [AttributeUsage(AttributeTargets.All)]
    public class RichEnumDisplayCategoryAttribute : PropertyAttribute {
        public string Category { get; }

        public RichEnumDisplayCategoryAttribute(string category) {
            Category = category;
        }
    }
}
