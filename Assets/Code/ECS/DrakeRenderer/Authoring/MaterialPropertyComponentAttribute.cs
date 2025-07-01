using System;
using System.Collections.Generic;
using Awaken.Utility.SerializableTypeReference;
using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MaterialPropertyComponentAttribute : SerializableTypeConstraintAttribute {
        static readonly HashSet<Type> AllowedTypes = ObtainAllowedTypes();

        public MaterialPropertyComponentAttribute() : base() {
            ShowShortName = true;
        }

        /// <inheritdoc/>
        public override bool IsConstraintSatisfied(Type type) {
            return base.IsConstraintSatisfied(type) && AllowedTypes.Contains(type);
        }

        static HashSet<Type> ObtainAllowedTypes() {
            var types = new HashSet<Type>();
            foreach (var t in TypeManager.GetAllTypes()) {
                if (t.Type == null) {
                    continue;
                }
                if (TypeManager.IsSharedComponentType(t.TypeIndex)) {
                    continue;
                }
                if (t.Type.GetCustomAttributes(typeof(MaterialPropertyAttribute), false).Length > 0) {
                    types.Add(t.Type);
                }
            }
            return types;
        }
    }
}