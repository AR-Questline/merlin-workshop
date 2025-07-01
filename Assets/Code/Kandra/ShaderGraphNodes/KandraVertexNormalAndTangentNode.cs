using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace KandraRenderer.ShaderGraphNodes {
    [Title("Input", "Geometry", Name)]
    sealed class KandraVertexNormalAndTangentNode : KandraVertexInputNode, IMayRequireVertexID, IMayRequireNormal, IMayRequireTangent {
        const string Name = "Internal Kandra Normal And Tangent";
        
        const int OutputIdNormal = 0;
        const int OutputIdTangent = 1;
            
        const string OutputSlotNameNormal = "Normal";
        const string OutputSlotNameTangent = "Tangent";

        public KandraVertexNormalAndTangentNode() {
            name = Name;
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization() {
            AddSlot(new Vector3MaterialSlot(OutputIdNormal, OutputSlotNameNormal, OutputSlotNameNormal, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(OutputIdTangent, OutputSlotNameTangent, OutputSlotNameTangent, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { OutputIdNormal, OutputIdTangent }, true);
        }
        
        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            return true;
        }
        
        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            if (stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All) {
                return Space switch {
                    CoordinateSpace.Object => NeededCoordinateSpace.Object,
                    CoordinateSpace.World => NeededCoordinateSpace.World,
                    _ => NeededCoordinateSpace.None
                };
            } else {
                return NeededCoordinateSpace.None;
            }
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All) {
            if (stageCapability is ShaderStageCapability.Vertex or ShaderStageCapability.All) {
                return Space switch {
                    CoordinateSpace.Object => NeededCoordinateSpace.Object,
                    CoordinateSpace.World => NeededCoordinateSpace.World,
                    _ => NeededCoordinateSpace.None
                };
            } else {
                return NeededCoordinateSpace.None;
            }
        }
        
        protected override void GenerateKandraNodeCode(ShaderStringBuilder sb) {
            var nameNormal = GetVariableNameForSlot(OutputIdNormal);
            var nameTangent = GetVariableNameForSlot(OutputIdTangent);
            sb.AppendLine("$precision3 {0} = 0;", nameNormal);
            sb.AppendLine("$precision3 {0} = 0;", nameTangent);
            sb.AppendLine("{0}(IN.VertexID, {1}, {2});", GetFunctionName(), nameNormal, nameTangent);
        }

        protected override void GenerateFallbackNodeCode(ShaderStringBuilder sb) {
            var nameNormal = GetVariableNameForSlot(OutputIdNormal);
            var nameTangent = GetVariableNameForSlot(OutputIdTangent);
            var variableNormal = Space switch {
                CoordinateSpace.World => "WorldSpaceNormal",
                CoordinateSpace.Object => "ObjectSpaceNormal",
                _ => throw new ArgumentOutOfRangeException()
            };
            var variableTangent = Space switch {
                CoordinateSpace.World => "WorldSpaceTangent",
                CoordinateSpace.Object => "ObjectSpaceTangent",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            sb.AppendLine("$precision3 {0} = IN.{1};", nameNormal, variableNormal);
            sb.AppendLine("$precision3 {0} = IN.{1};", nameTangent, variableTangent);
        }

        protected override void GenerateKandraFunction(FunctionRegistry registry) {
            var functionName = GetFunctionName();
            var sampleFunctionName = GetSampleFunctionName();
            registry.ProvideFunction(functionName, sb => {
                sb.AppendLine("void {0}(uint vertexID, out $precision3 outNormal, out $precision3 outTangent)", functionName);
                sb.AppendLine("{");
                using (sb.IndentScope()) {
                    sb.AppendLine($"uint2 instanceData = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP({InstanceDataReferenceName}, float2));");
                    sb.AppendLine("outPosition = 0;");
                    sb.AppendLine("outTangent = 0;");
                    sb.AppendLine("{0}(vertexID, instanceData, outNormal, outTangent);", sampleFunctionName);
                }
                sb.AppendLine("}");
            });
        }
        
        string GetFunctionName() {
            return Space switch {
                CoordinateSpace.World => "Kandra_Sample_NormalAndTangent_World_$precision",
                CoordinateSpace.Object => "Kandra_Sample_NormalAndTangent_Object_$precision",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        string GetSampleFunctionName() {
            return Space switch {
                CoordinateSpace.World => "SampleNormalAndTangentWorld",
                CoordinateSpace.Object => "SampleNormalAndTangentObject",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}