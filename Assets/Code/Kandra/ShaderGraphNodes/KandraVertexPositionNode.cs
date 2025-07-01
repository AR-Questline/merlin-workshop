using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace KandraRenderer.ShaderGraphNodes {
    [Title("Input", "Geometry", Name)]
    sealed class KandraVertexPositionNode : KandraVertexInputNode, IMayRequireVertexID, IMayRequirePosition, IMayRequireMeshUV {
        const string Name = "Internal Kandra Position";
        
        const int OutputIdPosition = 0;
        
        const string OutputSlotNamePosition = "Position";

        public KandraVertexPositionNode() {
            name = Name;
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization() {
            AddSlot(new Vector3MaterialSlot(OutputIdPosition, OutputSlotNamePosition, OutputSlotNamePosition, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { OutputIdPosition }, true);
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return true;
        }
        
        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return Space switch {
                CoordinateSpace.Object => NeededCoordinateSpace.Object,
                CoordinateSpace.World => NeededCoordinateSpace.World,
                _ => NeededCoordinateSpace.None
            };
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return channel == UVChannel.UV2;
        }
        
        protected override void GenerateKandraNodeCode(ShaderStringBuilder sb) {
            var namePosition = GetVariableNameForSlot(OutputIdPosition);
            sb.AppendLine("$precision3 {0} = 0;", namePosition);
            sb.AppendLine("{0}(IN.VertexID, {1});", GetNodeFunctionName(), namePosition);
        }

        protected override void GenerateFallbackNodeCode(ShaderStringBuilder sb) {
            var namePosition = GetVariableNameForSlot(OutputIdPosition);
            var variable = Space switch {
                CoordinateSpace.World => "WorldSpacePosition",
                CoordinateSpace.Object => "ObjectSpacePosition",
                _ => throw new ArgumentOutOfRangeException()
            };
            sb.AppendLine("$precision3 {0} = IN.{1};", namePosition, variable);
        }

        protected override void GenerateKandraFunction(FunctionRegistry registry) {
            var functionName = GetNodeFunctionName();
            var sampleFunctionName = GetSampleFunctionName();
            registry.ProvideFunction(functionName, sb => {
                sb.AppendLine("void {0}(uint vertexID, out $precision3 outPosition)", functionName);
                sb.AppendLine("{");
                using (sb.IndentScope()) {
                    sb.AppendLine($"uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP({InstanceDataReferenceName}, float2));");
                    sb.AppendLine("outPosition = 0;");
                    sb.AppendLine("{0}(vertexID, instanceData, outPosition);", sampleFunctionName);
                }
                sb.AppendLine("}");
            });
        }
        
        string GetNodeFunctionName() {
            return Space switch {
                CoordinateSpace.World => "Kandra_Sample_Position_World_$precision",
                CoordinateSpace.Object => "Kandra_Sample_Position_Object_$precision",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        string GetSampleFunctionName() {
            return Space switch {
                CoordinateSpace.World => "SamplePositionWorld",
                CoordinateSpace.Object => "SamplePositionObject",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}