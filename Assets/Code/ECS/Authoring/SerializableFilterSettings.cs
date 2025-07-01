using System;
using Awaken.ECS.Utils;
using Awaken.Utility;
using Unity.Entities;
using Unity.Entities.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Authoring {
    [Serializable]
    public struct SerializableFilterSettings : IEquatable<SerializableFilterSettings> {
        [LayerField] public int Layer;
        public uint RenderingLayerMask;
        public byte MotionMode;
        public byte ShadowCastingMode;
        public BlittableBool ReceiveShadows;
        public BlittableBool StaticShadowCaster;

        public RenderFilterSettings ToRenderFilterSettings() {
            return new RenderFilterSettings {
                Layer = Layer,
                RenderingLayerMask = RenderingLayerMask,
                MotionMode = (MotionVectorGenerationMode)(int)MotionMode,
                ShadowCastingMode = (ShadowCastingMode)(int)ShadowCastingMode,
                ReceiveShadows = ReceiveShadows,
                StaticShadowCaster = StaticShadowCaster
            };
        }

        public bool Equals(SerializableFilterSettings other) {
            return Layer == other.Layer &&
                   RenderingLayerMask == other.RenderingLayerMask &&
                   MotionMode == other.MotionMode &&
                   ShadowCastingMode == other.ShadowCastingMode &&
                   ReceiveShadows == other.ReceiveShadows &&
                   StaticShadowCaster == other.StaticShadowCaster;
        }

        public override bool Equals(object obj) {
            return obj is SerializableFilterSettings other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = Layer;
                hashCode = (hashCode * 397) ^ (int)RenderingLayerMask;
                hashCode = (hashCode * 397) ^ (int)MotionMode;
                hashCode = (hashCode * 397) ^ (int)ShadowCastingMode;
                hashCode = (hashCode * 397) ^ ReceiveShadows.GetHashCode();
                hashCode = (hashCode * 397) ^ StaticShadowCaster.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SerializableFilterSettings left, SerializableFilterSettings right) {
            return left.Equals(right);
        }

        public static bool operator !=(SerializableFilterSettings left, SerializableFilterSettings right) {
            return !left.Equals(right);
        }
    }
}
