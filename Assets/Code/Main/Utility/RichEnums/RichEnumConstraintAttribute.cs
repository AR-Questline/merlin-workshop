// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Utility.RichEnums {
    /// <summary>
    /// Constraint that allows selection of rich enums that are contained within specific class when
    /// selecting a <see cref="RichEnumReference"/> with the Unity inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class RichEnumExtendsAttribute : PropertyAttribute {
        /// <summary>
        /// Initializes a new instance of the <see cref="RichEnumExtendsAttribute"/> class.
        /// </summary>
        public RichEnumExtendsAttribute(Type baseType, bool showOthers = false) {
            BaseTypes = new[] {baseType};
            ShowOthers = showOthers;
        }

        public RichEnumExtendsAttribute(Type baseType, string[] inspectorCategories, bool displayOnlyEnumName = false) {
            BaseTypes = new[] {baseType};
            InspectorCategories = inspectorCategories;
            OnlyEnumNames = displayOnlyEnumName;
        }

        public RichEnumExtendsAttribute(params Type[] types) {
            BaseTypes = types;
        }

        /// <summary>
        /// Gets the type of rich enum that selectable classes must derive from.
        /// </summary>
        public Type[] BaseTypes { get; }
        
        /// <summary>
        /// Should types that don't satisfy constraint be showed in separate directory?
        /// </summary>
        public bool ShowOthers { get; }
        
        /// <summary>
        /// Array with desired inspector categories to display
        /// </summary>
        public string[] InspectorCategories { get; }
        
        /// <summary>
        /// Prevents nesting in drawer categories and displays only enum names
        /// </summary>
        public bool OnlyEnumNames { get; }

        public bool IsConstraintSatisfied(Type type) {
            return BaseTypes.Any(t => t.IsAssignableFrom(type));
        }

    }
}