using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.RichEnums {
    [AttributeUsage(AttributeTargets.Field)]
    [UnityEngine.Scripting.Preserve]
    public class RichEnumNodeExtendsAttribute : PropertyAttribute {
        /// <summary>
        /// Skill graph version of <see cref="RichEnumExtendsAttribute"/>
        /// </summary>
        public RichEnumNodeExtendsAttribute(Type baseType) {
            BaseType = baseType;
        }

        /// <summary>
        /// Gets the type of rich enum that selectable classes must derive from.
        /// </summary>
        public Type BaseType { get; }

        public static explicit operator RichEnumExtendsAttribute(RichEnumNodeExtendsAttribute t) {
            return new RichEnumExtendsAttribute(t.BaseType);
        }
    }
}