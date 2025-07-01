using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace KandraRenderer.ShaderGraphNodes {
    abstract class KandraVertexInputNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction {
        protected const string InstanceDataReferenceName = "_InstanceData";

        [SerializeField] CoordinateSpace space;
        protected CoordinateSpace Space => space;
        
        [PopupControl("Space")]
        public PopupList SpacePopup {
            get => new(SpacePopupValues, (int)space);
            set {
                if (space != (CoordinateSpace)value.selectedEntry) {
                    space = (CoordinateSpace)value.selectedEntry;
                    Dirty(ModificationScope.Graph);
                }
            }
        }
        
        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode) {
            properties.AddShaderProperty(new Vector2ShaderProperty() {
                displayName = "Instance data",
                overrideReferenceName = InstanceDataReferenceName,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                hidden = true,
                value = new Vector2(0, 0),
                precision = Precision.Single,
            });

            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode) {
            sb.AppendLine("#if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))");
            if (generationMode == GenerationMode.ForReals) {
                GenerateKandraNodeCode(sb);
            } else {
                GenerateFallbackNodeCode(sb);
            }
            sb.AppendLine("#else");
            GenerateFallbackNodeCode(sb);
            sb.AppendLine("#endif");
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode) {
            registry.ProvideFunction("includeSampleSkinBuffer", sb => {
                // Comment mutes function-not-provided warning
                sb.AppendLine("// includeSampleSkinBuffer");
                sb.AppendLine("#include \"Assets/Code/Kandra/ShaderGraphNodes/SampleSkinBuffer.hlsl\"");
            });
            
            GenerateKandraFunction(registry);
        }

        protected abstract void GenerateKandraNodeCode(ShaderStringBuilder sb);
        protected abstract void GenerateFallbackNodeCode(ShaderStringBuilder sb);

        protected abstract void GenerateKandraFunction(FunctionRegistry registry);
        

        static readonly string[] SpacePopupValues = { "World", "Object" };
        protected enum CoordinateSpace : byte {
            World,
            Object,
        }
    }
}