using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Utility.Graphics {
    public class MaterialUtils {
        static readonly HashSet<string> ExcludedProperties = new HashSet<string>() {
            "RenderType", "_CullMode", "_BlendMode", "_DisplacementMode", "_DoubleSidedEnable", "_DoubleSidedNormalMode",
            "_ZWrite", "_ZTestTransparent", "_ZTestModeDistortion", "_ZTestGBuffer", "_TransparentCullMode",
            "_TransparentZWrite", "_SurfaceType", "_RefractionModel", "_AddPrecomputedVelocity", "_AlphaCutoffEnable",
            "_AlphaDstBlend", "_AlphaSrcBlend", "_Anisotropy", "_CoatMask", "_CullModeForward", "_DepthOffsetEnable",
            "_DiffusionProfile", "_DiffusionProfileHash", "_DisplacementMode", "_DistortionBlendMode", "_DistortionBlurBlendMode",
            "_DistortionEnable", "_DstBlend", "_EmissiveColorMode", "_EnableBlendModePreserveSpecularLighting", "_EnableFogOnTransparent",
            "_EnableGeometricSpecularAA", "_EnergyConservingSpecularColor", "_IridescenceMask", "_MaterialID", "_ReceivesSSR",
            "_RefractionModel", "_SSRefractionProjectionModel", "_SpecularOcclusionMode", "_SrcBlend", "_StencilRef", "_StencilRefDepth",
            "_StencilRefDistortionVec", "_StencilRefGBuffer", "_StencilRefMV", "_StencilWriteMask", "_StencilWriteMaskDepth",
            "_StencilWriteMaskDistortionVec", "_StencilWriteMaskGBuffer", "_StencilWriteMaskMV", "_SubsurfaceMask", "_SupportDecals",
            "_TransmissionEnable", "_TransparentBackfaceEnable", "_TransparentDepthPostpassEnable", "_TransparentDepthPrepassEnable",
            "_TransparentSortPriority", "_TransparentWritingMotionVec", "_UseEmissiveIntensity", "_UseShadowThreshold",
            "_ZTestDepthEqualForOpaque"
        };
        
        public static Material CopyMaterial(Material oldMaterial, Material newMaterial, List<string> forcedProperties = null) {
            var destination = new Material(newMaterial);
            forcedProperties ??= new List<string>();
            
            HashSet<ShaderProperty> destinationProperties = new HashSet<ShaderProperty>();
            var destinationShader = destination.shader;
            var destinationPropertyCount = destination.shader.GetPropertyCount();
            for (int i = 0; i < destinationPropertyCount; i++) {
                var propName = destinationShader.GetPropertyName(i);
                if (!ExcludedProperties.Contains(propName) || forcedProperties.Contains(propName)) {
                    destinationProperties.Add(new ShaderProperty(propName, destinationShader.GetPropertyType(i)));
                }
            }

            var oldShader = oldMaterial.shader;
            foreach (ShaderProperty shaderProperty in destinationProperties) {
                if (oldMaterial.HasProperty(shaderProperty.nameId) && shaderProperty.Exists(oldShader)) {
                    shaderProperty.Copy(oldMaterial, destination);
                }
            }

            return destination;
        }

        readonly struct ShaderProperty {
            public readonly string name;
            public readonly int nameId;
            public readonly ShaderPropertyType propertyType;
            
            public ShaderProperty(string name, ShaderPropertyType propertyType) {
                this.name = name;
                this.nameId = Shader.PropertyToID(name);
                this.propertyType = propertyType;
            }
            
            // === Operations
            public bool Exists(Shader shader) {
                var index = shader.FindPropertyIndex(name);
                if (index >= 0 && index < shader.GetPropertyCount()) {
                    return propertyType == shader.GetPropertyType(index);
                }

                return false;
            }

            public void Copy(Material from, Material to) {
                if (propertyType == ShaderPropertyType.Color) {
                    to.SetColor(nameId, from.GetColor(nameId));
                }else if (propertyType == ShaderPropertyType.Float) {
                    to.SetFloat(nameId, from.GetFloat(nameId));
                }else if (propertyType == ShaderPropertyType.Texture) {
                    to.SetTexture(nameId, from.GetTexture(nameId));
                }else if (propertyType == ShaderPropertyType.Vector) {
                    to.SetVector(nameId, from.GetVector(nameId));
                }
            }

            // === Equality
            public bool Equals(ShaderProperty other) {
                return nameId == other.nameId && propertyType == other.propertyType;
            }

            public override bool Equals(object obj) {
                return obj is ShaderProperty other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    return (nameId * 397) ^ (int) propertyType;
                }
            }

            public static bool operator ==(ShaderProperty left, ShaderProperty right) {
                return left.Equals(right);
            }

            public static bool operator !=(ShaderProperty left, ShaderProperty right) {
                return !left.Equals(right);
            }
        }
    }
}