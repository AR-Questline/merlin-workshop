using System;
using Unity.IL2CPP.CompilerServices;
using UnityEngine.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [Serializable, Il2CppEagerStaticClassConstruction]
    public struct DrakeRendererArchetypeKey : IEquatable<DrakeRendererArchetypeKey> {
        public static readonly DrakeRendererArchetypeKey[] All = CreateAllValues();

        public bool isStatic;
        public bool isTransparent;
        [NonSerialized] public bool hasLodGroup;
        public bool inMotionPass;
        [NonSerialized] public bool hasShadowsOverriden;
        [NonSerialized] public bool hasLocalToWorldOffset;
        public LightProbeUsage lightProbeUsage;

        public DrakeRendererArchetypeKey(bool isStatic, bool isTransparent, bool hasLodGroup, bool inMotionPass, LightProbeUsage lightProbeUsage, bool hasShadowsOverriden, bool hasLocalToWorldOffset) {
            this.isStatic = isStatic;
            this.isTransparent = isTransparent;
            this.hasLodGroup = hasLodGroup;
            this.lightProbeUsage = lightProbeUsage;
            this.inMotionPass = inMotionPass;
            this.hasShadowsOverriden = hasShadowsOverriden;
            this.hasLocalToWorldOffset = hasLocalToWorldOffset;
        }
        
        public bool Equals(DrakeRendererArchetypeKey other) {
            return isStatic == other.isStatic &&
                   isTransparent == other.isTransparent &&
                   hasLodGroup == other.hasLodGroup &&
                   inMotionPass == other.inMotionPass &&
                   lightProbeUsage == other.lightProbeUsage &&
                   hasShadowsOverriden == other.hasShadowsOverriden &&
                   hasLocalToWorldOffset == other.hasLocalToWorldOffset;
        }
        public override bool Equals(object obj) {
            return obj is DrakeRendererArchetypeKey other && Equals(other);
        }
        public override int GetHashCode() {
            unchecked {
                int hashCode = isStatic.GetHashCode();
                hashCode = (hashCode * 397) ^ isTransparent.GetHashCode();
                hashCode = (hashCode * 397) ^ hasLodGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ inMotionPass.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)lightProbeUsage;
                hashCode = (hashCode * 397) ^ hasShadowsOverriden.GetHashCode();
                hashCode = (hashCode * 397) ^ hasLocalToWorldOffset.GetHashCode();
                return hashCode;
            }
        }
        public static bool operator ==(DrakeRendererArchetypeKey left, DrakeRendererArchetypeKey right) {
            return left.Equals(right);
        }
        public static bool operator !=(DrakeRendererArchetypeKey left, DrakeRendererArchetypeKey right) {
            return !left.Equals(right);
        }

        static DrakeRendererArchetypeKey[] CreateAllValues() {
            // _static * _isTransparent * _hasLod * _inMotionPass * _lightProbeUsage * _hasShadowsOverriden * _hasLocalToWorldOffset
            DrakeRendererArchetypeKey[] values = new DrakeRendererArchetypeKey[2*2*2*2*4*2*2];
            int index = 0;
            for (int i = 0; i < 2; i++) {
                var isStatic = i == 1;
                for (int j = 0; j < 2; j++) {
                    var isTransparent = j == 1;
                    for (int k = 0; k < 2; k++) {
                        var hasLod = k == 1;
                        for (int l = 0; l < 2; l++) {
                            var inMotion = l == 1;
                            for (int m = 0; m < 4; m++) {
                                var lightProbeUsage = m == 0 ? LightProbeUsage.Off : (LightProbeUsage)(1 << (m-1));
                                for (int n = 0; n < 2; n++) {
                                    var hasShadowsOverriden = n == 1;
                                    for (int o = 0; o < 2; o++) {
                                        var hasLocalToWorldOffset = o == 1;
                                        values[index++] = new DrakeRendererArchetypeKey(isStatic, isTransparent, hasLod, inMotion, lightProbeUsage, hasShadowsOverriden, hasLocalToWorldOffset);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return values;
        }
        public DrakeRendererArchetypeKey OverrideIsStatic(bool? isStaticOverride) {
            if (isStaticOverride.HasValue) {
                var staticOverride = isStaticOverride.Value;
                if (staticOverride) {
                    return new DrakeRendererArchetypeKey(true, this.isTransparent, this.hasLodGroup, false, this.lightProbeUsage, this.hasShadowsOverriden, this.hasLocalToWorldOffset);
                } else {
                    return new DrakeRendererArchetypeKey(false, this.isTransparent, this.hasLodGroup, this.inMotionPass, this.lightProbeUsage, this.hasShadowsOverriden, this.hasLocalToWorldOffset);
                }
            }
            return this;
        }
    }
}
