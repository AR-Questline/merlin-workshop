// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 形状に合わせた頂点リダクション
    /// このリダクションは頂点の接続状態に沿ってリダクションを行う
    /// </summary>
    public class ShapeDistanceReduction : StepReductionBase
    {
        //=========================================================================================
        public ShapeDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float startMergeLength,
            float endMergeLength,
            int maxStep,
            bool dontMakeLine,
            float joinPositionAdjustment
            )
            : base($"ShapeReduction [{name}]", mesh, workingData, startMergeLength, endMergeLength, maxStep, dontMakeLine, joinPositionAdjustment)
        {
        }

        public override void Dispose()
        {
        }

        protected override void StepInitialize()
        {
        }

        protected override void CustomReductionStep()
        {
        }

        [BurstCompile]
        struct SearchJoinEdgeJob : IJob
        {
            public int vcnt;
            public float radius;
            public bool dontMakeLine;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            //public NativeArray<ExFixedSet128Bytes<ushort>> vertexToVertexArray;

            public NativeList<JoinEdge> joinEdgeList;

            public void Execute()
            {
            }
        }
    }
}
