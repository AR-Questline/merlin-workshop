using System;
using System.Collections.Generic;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Authoring {
    public struct SerializableRenderMeshDescription : IEquatable<SerializableRenderMeshDescription> {
        public static readonly IEqualityComparer<SerializableRenderMeshDescription> Comparer = new EqualityComparer();
        public LightProbeUsage LightProbeUsage => throw new NotImplementedException();

        public SerializableRenderMeshDescription(Renderer renderer) {
        }

        public void OverrideLayer(int layer) {
        }

        public void OverrideRenderingLayerMask(uint renderingLayerMask) {
        }

        public void OverrideShadowsCasting(ShadowCastingMode castingMode) {
        }

        public readonly RenderMeshDescription ToRenderMeshDescription(bool isStatic) {
            throw new NotImplementedException();
        }

        public readonly bool Equals(SerializableRenderMeshDescription other) {
            throw new NotImplementedException();
        }

        public readonly override int GetHashCode() {
            throw new NotImplementedException();
        }

        sealed class EqualityComparer : IEqualityComparer<SerializableRenderMeshDescription> {
            public bool Equals(SerializableRenderMeshDescription x, SerializableRenderMeshDescription y) {
                throw new NotImplementedException();
            }

            public int GetHashCode(SerializableRenderMeshDescription obj) {
                throw new NotImplementedException();
            }
        }
    }
}