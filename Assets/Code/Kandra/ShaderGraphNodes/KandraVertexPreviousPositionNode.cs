using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace KandraRenderer.ShaderGraphNodes {
    [Title("Input", "Kandra sample previous position")]
    sealed class KandraVertexPreviousPositionNode : AbstractMaterialNode, IMayRequireVertexID, IGeneratesBodyCode, IGeneratesFunction {
        const int PositionOutputSlotId = 0;

        const string OutputSlotPositionName = "Previous Position";
        const string InstanceDataReferenceName = "_InstanceData";

        public KandraVertexPreviousPositionNode() {
            name = "Kandra sample previous position";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization() {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, OutputSlotPositionName, OutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));

            RemoveSlotsNameNotMatching(new[] { PositionOutputSlotId }, true);
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return true;
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

        // This generates the code that calls our functions.
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode) {
            sb.AppendLine("#if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))");
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(PositionOutputSlotId));
            if (generationMode == GenerationMode.ForReals) {
                sb.AppendLine($"{GetFunctionName()}(IN.VertexID, {GetVariableNameForSlot(PositionOutputSlotId)});");
            }

            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(PositionOutputSlotId));
            sb.AppendLine("#endif");
        }

        // This generates our functions, and is outside any function scope.
        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode) {
            registry.ProvideFunction("includeSampleSkinBuffer", sb => {
                // Comment mutes function-not-provided warning
                sb.AppendLine("// includeSampleSkinBuffer");
                sb.AppendLine("#include \"Assets/Code/Kandra/ShaderGraphNodes/SampleSkinBuffer.hlsl\"");
            });

            registry.ProvideFunction(GetFunctionName(), sb => {
                sb.AppendLine($"#ifndef PREVENT_REPEAT_PREVIOUS_POSITION_SAMPLE");
                sb.AppendLine($"#define PREVENT_REPEAT_PREVIOUS_POSITION_SAMPLE");
                sb.AppendLine($"void {GetFunctionName()}(uint vertexId, out $precision3 positionOut)");
                sb.AppendLine("{");
                using (sb.IndentScope()) {
                    sb.AppendLine($"uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP({InstanceDataReferenceName}, float2));");
                    sb.AppendLine("positionOut = 0;");
                    sb.AppendLine("SamplePreviousPosition(vertexId, instanceData, positionOut);");
                }

                sb.AppendLine("}");
                sb.AppendLine("#endif");
            });
        }

        string GetFunctionName() {
            return "Sample_Previous_Position_Buffer_$precision";
        }
    }
}
