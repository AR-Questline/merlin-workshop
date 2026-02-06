using System;
using UnityEngine.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public struct DrakeRendererArchetypeKey : IEquatable<DrakeRendererArchetypeKey> {
        public static readonly DrakeRendererArchetypeKey[] All;
        public bool isStatic;
        public bool isTransparent;
        public bool hasLodGroup;
        public bool inMotionPass;
        public bool hasShadowsOverriden;
        public bool hasLocalToWorldOffset;
        public LightProbeUsage lightProbeUsage;

        public DrakeRendererArchetypeKey(bool isStatic, bool isTransparent, bool hasLodGroup, bool inMotionPass,
            LightProbeUsage lightProbeUsage, bool hasShadowsOverriden, bool hasLocalToWorldOffset) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeRendererArchetypeKey other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(DrakeRendererArchetypeKey left, DrakeRendererArchetypeKey right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeRendererArchetypeKey left, DrakeRendererArchetypeKey right) {
            throw new NotImplementedException();
        }

        public DrakeRendererArchetypeKey OverrideIsStatic(bool? isStaticOverride) {
            throw new NotImplementedException();
        }
    }
}