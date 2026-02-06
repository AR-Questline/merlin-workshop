using System;
using Awaken.Utility.SerializableTypeReference;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public sealed class MaterialPropertyComponentAttribute : SerializableTypeConstraintAttribute {
        public MaterialPropertyComponentAttribute() {
            throw new NotImplementedException();
        }

        public override bool IsConstraintSatisfied(Type type) {
            throw new NotImplementedException();
        }
    }
}