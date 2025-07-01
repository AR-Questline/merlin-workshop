using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace KandraRenderer.ShaderGraphNodes {
    [Title("Input", "Sample skin buffer")]
    class SampleSkinBufferNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexID,
        IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireMeshUV {
        const int PositionOutputSlotId = 0;
        const int NormalOutputSlotId = 1;
        const int TangentOutputSlotId = 2;

        const string OutputSlotPositionName = "Deformed Position";
        const string OutputSlotNormalName = "Deformed Normal";
        const string OutputSlotTangentName = "Deformed Tangent";
        const string InstanceDataReferenceName = "_InstanceData";

        public SampleSkinBufferNode() {
            name = "Sample skin buffer";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization() {
            AddSlot(new Vector3MaterialSlot(PositionOutputSlotId, OutputSlotPositionName, OutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(NormalOutputSlotId, OutputSlotNormalName, OutputSlotNormalName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(TangentOutputSlotId, OutputSlotTangentName, OutputSlotTangentName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));

            RemoveSlotsNameNotMatching(new[] { PositionOutputSlotId, NormalOutputSlotId, TangentOutputSlotId }, true);
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

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            if (stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All) {
                return NeededCoordinateSpace.Object;
            } else {
                return NeededCoordinateSpace.None;
            }
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            if (stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All) {
                return NeededCoordinateSpace.Object;
            } else {
                return NeededCoordinateSpace.None;
            }
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            if (stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All) {
                return NeededCoordinateSpace.Object;
            } else {
                return NeededCoordinateSpace.None;
            }
        }

        public bool RequiresMeshUV(UVChannel channel,
            ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return channel == UVChannel.UV0 &&
                   stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All;
        }

        // This generates the code that calls our functions.
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode) {
            sb.AppendLine("#if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))");
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(PositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(NormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(TangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals) {
                sb.AppendLine($"{GetFunctionName()}(IN.VertexID, " +
                              $"{GetVariableNameForSlot(PositionOutputSlotId)}, " +
                              $"{GetVariableNameForSlot(NormalOutputSlotId)}, " +
                              $"{GetVariableNameForSlot(TangentOutputSlotId)});");
            }

            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;", GetVariableNameForSlot(PositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;", GetVariableNameForSlot(NormalOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceTangent;", GetVariableNameForSlot(TangentOutputSlotId));
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
                sb.AppendLine($"#ifndef PREVENT_REPEAT_SKIN_SAMPLE");
                sb.AppendLine($"#define PREVENT_REPEAT_SKIN_SAMPLE");
                sb.AppendLine($"void {GetFunctionName()}(uint vertexId, " +
                              "out $precision3 positionOut, " +
                              "out $precision3 normalOut, " +
                              "out $precision3 tangentOut)");
                sb.AppendLine("{");
                using (sb.IndentScope()) {
                    sb.AppendLine($"uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP({InstanceDataReferenceName}, float2));");
                    sb.AppendLine("positionOut = 0;");
                    sb.AppendLine("normalOut = 0;");
                    sb.AppendLine("tangentOut = 0;");
                    sb.AppendLine("sampleDeform(vertexId, instanceData, positionOut, normalOut, tangentOut);");
                }

                sb.AppendLine("}");
                sb.AppendLine("#endif");
            });
        }

        string GetFunctionName() {
            return "Sample_Skin_Buffer_$precision";
        }
    }
}