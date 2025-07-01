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
    /// 単純な頂点間の距離によるリダクション
    /// このリダクションは頂点の接続状態を無視する
    /// </summary>
    public class SimpleDistanceReduction : StepReductionBase
    {
        /// <summary>
        /// グリッドマップ
        /// </summary>
        private GridMap<int> gridMap;

        //=========================================================================================
        public SimpleDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float startMergeLength,
            float endMergeLength,
            int maxStep,
            bool dontMakeLine,
            float joinPositionAdjustment
            )
            : base($"SimpleDistanceReduction [{name}]", mesh, workingData, startMergeLength, endMergeLength, maxStep, dontMakeLine, joinPositionAdjustment)
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
        struct InitGridJob : IJob
        {
            public int vcnt;
            public float gridSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            public NativeParallelMultiHashMap<int3, int> gridMap;

            // 頂点ごと
            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct SearchJoinEdgeJob : IJob
        {
            public int vcnt;
            public float gridSize;
            public float radius;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            [Unity.Collections.WriteOnly]
            public NativeList<JoinEdge> joinEdgeList;

            // 頂点ごと
            public void Execute()
            {
            }
        }
    }
}
