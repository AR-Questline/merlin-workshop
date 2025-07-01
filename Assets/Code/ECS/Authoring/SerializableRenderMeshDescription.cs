using System;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Authoring {
    /// <summary>
    /// Serializable version of <see cref="Unity.Rendering.RenderMeshDescription"/>
    /// </summary>
    [Serializable, Il2CppEagerStaticClassConstruction]
    public struct SerializableRenderMeshDescription : IEquatable<SerializableRenderMeshDescription> {
        [UnityEngine.Scripting.Preserve] public static readonly IEqualityComparer<SerializableRenderMeshDescription> Comparer = new EqualityComparer();

        [SerializeField] internal int layer;
        [SerializeField] internal uint renderingLayer;
        [SerializeField] internal ShadowCastingMode shadowCastingMode;
        [SerializeField] internal bool receiveShadows;
        [SerializeField] internal MotionVectorGenerationMode motionVectorGenerationMode;
        [SerializeField] internal bool staticShadowCaster;
        [SerializeField] internal LightProbeUsage lightProbeUsage;

        public LightProbeUsage LightProbeUsage => lightProbeUsage;
        
        public SerializableRenderMeshDescription(Renderer renderer) {
            RenderMeshDescription desc = new RenderMeshDescription(renderer);
            layer = desc.FilterSettings.Layer;
            renderingLayer = desc.FilterSettings.RenderingLayerMask;
            shadowCastingMode = desc.FilterSettings.ShadowCastingMode;
            receiveShadows = desc.FilterSettings.ReceiveShadows;
            staticShadowCaster = desc.FilterSettings.StaticShadowCaster;
            lightProbeUsage = desc.LightProbeUsage;
            motionVectorGenerationMode = desc.FilterSettings.MotionMode;
        }

        public void OverrideLayer(int layer) {
            this.layer = layer;
        }

        public void OverrideRenderingLayerMask(uint renderingLayerMask) {
            this.renderingLayer = renderingLayerMask;
        }

        public void OverrideShadowsCasting(ShadowCastingMode castingMode) {
            this.shadowCastingMode = castingMode;
        }

        public readonly RenderMeshDescription ToRenderMeshDescription(bool isStatic) {
            var motionVectorMode = motionVectorGenerationMode;
            if (isStatic && motionVectorMode == MotionVectorGenerationMode.Object) {
                motionVectorMode = MotionVectorGenerationMode.Camera;
            }
            return new(shadowCastingMode: shadowCastingMode,
                receiveShadows: receiveShadows,
                motionVectorGenerationMode: motionVectorMode,
                layer: layer,
                renderingLayerMask: renderingLayer,
                lightProbeUsage: lightProbeUsage,
                staticShadowCaster: staticShadowCaster);
        }

        public readonly bool Equals(SerializableRenderMeshDescription other) {
            return layer == other.layer &&
                   renderingLayer == other.renderingLayer &&
                   shadowCastingMode == other.shadowCastingMode &&
                   receiveShadows == other.receiveShadows &&
                   motionVectorGenerationMode == other.motionVectorGenerationMode &&
                   staticShadowCaster == other.staticShadowCaster &&
                   lightProbeUsage == other.lightProbeUsage;
        }

        public readonly override int GetHashCode() {
            unchecked {
                int hashCode = layer;
                hashCode = (hashCode * 397) ^ (int)renderingLayer;
                hashCode = (hashCode * 397) ^ (int)shadowCastingMode;
                hashCode = (hashCode * 397) ^ receiveShadows.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)motionVectorGenerationMode;
                hashCode = (hashCode * 397) ^ staticShadowCaster.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)lightProbeUsage;
                return hashCode;
            }
        }

        sealed class EqualityComparer : IEqualityComparer<SerializableRenderMeshDescription> {
            public bool Equals(SerializableRenderMeshDescription x, SerializableRenderMeshDescription y) {
                return x.Equals(y);
            }

            public int GetHashCode(SerializableRenderMeshDescription obj) {
                return obj.GetHashCode();
            }
        }
    }
}
