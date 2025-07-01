// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class SimulationManager : IManager, IValid
    {
        /// <summary>
        /// チームID
        /// </summary>
        public ExNativeArray<short> teamIdArray;

        /// <summary>
        /// 現在のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> nextPosArray;

        /// <summary>
        /// １つ前のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> oldPosArray;

        /// <summary>
        /// １つ前のシミュレーション回転(todo:現在未使用）
        /// </summary>
        public ExNativeArray<quaternion> oldRotArray;

        /// <summary>
        /// 現在のアニメーション姿勢座標
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<float3> basePosArray;

        /// <summary>
        /// 現在のアニメーション姿勢回転
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<quaternion> baseRotArray;

        /// <summary>
        /// １つ前の原点座標
        /// </summary>
        public ExNativeArray<float3> oldPositionArray;

        /// <summary>
        /// １つ前の原点回転
        /// </summary>
        public ExNativeArray<quaternion> oldRotationArray;

        /// <summary>
        /// 速度計算用座標
        /// </summary>
        public ExNativeArray<float3> velocityPosArray;

        /// <summary>
        /// 表示座標
        /// </summary>
        public ExNativeArray<float3> dispPosArray;

        /// <summary>
        /// 速度
        /// </summary>
        public ExNativeArray<float3> velocityArray;

        /// <summary>
        /// 実速度
        /// </summary>
        public ExNativeArray<float3> realVelocityArray;

        /// <summary>
        /// 摩擦(0.0 ~ 1.0)
        /// </summary>
        public ExNativeArray<float> frictionArray;

        /// <summary>
        /// 静止摩擦係数
        /// </summary>
        public ExNativeArray<float> staticFrictionArray;

        /// <summary>
        /// 接触コライダーの衝突法線
        /// </summary>
        public ExNativeArray<float3> collisionNormalArray;

        /// <summary>
        /// 接触中コライダーID
        /// 接触コライダーID+1が格納されているので注意！(0=なし)
        /// todo:現在未使用!
        /// </summary>
        //public ExNativeArray<int> colliderIdArray;

        public int ParticleCount => nextPosArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// 制約
        /// </summary>
        public DistanceConstraint distanceConstraint;
        public TriangleBendingConstraint bendingConstraint;
        public TetherConstraint tetherConstraint;
        public AngleConstraint angleConstraint;
        public InertiaConstraint inertiaConstraint;
        public ColliderCollisionConstraint colliderCollisionConstraint;
        public MotionConstraint motionConstraint;
        public SelfCollisionConstraint selfCollisionConstraint;

        //=========================================================================================
        /// <summary>
        /// フレームもしくはステップごとに変動するリストを管理するための汎用バッファ。用途は様々
        /// </summary>
        internal ExProcessingList<int> processingStepParticle;
        internal ExProcessingList<int> processingStepTriangleBending;
        internal ExProcessingList<int> processingStepEdgeCollision;
        internal ExProcessingList<int> processingStepCollider;
        internal ExProcessingList<int> processingStepBaseLine;
        //internal ExProcessingList<int> processingIntList5;
        internal ExProcessingList<int> processingStepMotionParticle;

        internal ExProcessingList<int> processingSelfParticle;
        internal ExProcessingList<uint> processingSelfPointTriangle;
        internal ExProcessingList<uint> processingSelfEdgeEdge;
        internal ExProcessingList<uint> processingSelfTrianglePoint;

        //---------------------------------------------------------------------
        /// <summary>
        /// 汎用float3作業バッファ
        /// </summary>
        internal NativeArray<float3> tempFloat3Buffer;

        /// <summary>
        /// パーティクルごとのfloat3集計カウンタ（排他制御用）
        /// </summary>
        internal NativeArray<int> countArray;

        /// <summary>
        /// パーティクルごとのfloat3蓄積リスト、内部は固定小数点。パーティクル数x3。（排他制御用）
        /// </summary>
        internal NativeArray<int> sumArray;

        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢座標
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<float3> stepBasicPositionBuffer;

        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢回転
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<quaternion> stepBasicRotationBuffer;

        /// <summary>
        /// ステップ実行カウンター
        /// </summary>
        internal int SimulationStepCount { get; private set; }

        /// <summary>
        /// 実行環境で利用できるワーカースレッド数
        /// </summary>
        internal int WorkerCount => Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Initialize()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        internal void RegisterProxyMesh(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void RegisterConstraint(ClothProcess cprocess)
        {
        }


        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        internal void ExitProxyMesh(ClothProcess cprocess)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 作業バッファの更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション実行前処理
        /// -リセット
        /// -移動影響
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PreSimulationUpdate(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct PreSimulationUpdateJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosArray;
            public NativeArray<float3> oldPosArray;
            public NativeArray<quaternion> oldRotArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotArray;
            public NativeArray<float3> oldPositionArray;
            public NativeArray<quaternion> oldRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityPosArray;
            public NativeArray<float3> dispPosArray;
            public NativeArray<float3> velocityArray;
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float> frictionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float> staticFrictionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> collisionNormalArray;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<int> colliderIdArray;

            // パーティクルごと
            public void Execute(int pindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーションの１ステップ実行
        /// </summary>
        /// <param name="updateCount"></param>
        /// <param name="updateIndex"></param>
        /// <param name="simulationDeltaTime"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SimulationStepUpdate(int updateCount, int updateIndex, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct ClearStepCounter : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepParticle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepTriangleBending;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepEdgeCollision;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepCollider;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepBaseLine;
            //[Unity.Collections.WriteOnly]
            //public NativeReference<int> processingCounter5;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingStepMotionParticle;

            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfParticle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfPointTriangle;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfEdgeEdge;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingSelfTrianglePoint;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct CreateUpdateParticleList : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepParticleIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepBaseLineIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepBaseLineIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepTriangleBendIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepTriangleBendIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> stepEdgeCollisionIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> stepEdgeCollisionIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> motionParticleIndexCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> motionParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfParticleCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> selfParticleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfPointTriangleCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfPointTriangleIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfEdgeEdgeCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfEdgeEdgeIndexArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> selfTrianglePointCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<uint> selfTrianglePointIndexArray;

            // チームごと
            public void Execute(int teamId)
            {
            }
        }

        [BurstCompile]
        struct StartSimulationStepJob : IJobParallelForDefer
        {
            public float4 simulationPower;
            public float simulationDeltaTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamWindData> teamWindArray;

            // wind
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotationArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> stepBasicPositionArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> stepBasicRotationArray;


            // ステップパーティクルごと
            public void Execute(int index)
            {
            }

            void Spring(in SpringConstraint.SpringConstraintParams springParams, ClothNormalAxis normalAxis, ref float3 nextPos, in float3 basePos, in quaternion baseRot, float noiseTime, float scaleRatio)
            {
            }

            float3 Wind(int teamId, in TeamManager.TeamData tdata, in WindParams windParams, in InertiaConstraint.CenterData cdata, int vindex, int pindex, float depth)
            {
                return default;
            }

            float3 WindForceBlend(in TeamWindInfo windInfo, in WindParams windParams, in float3 windPos, float windTurbulence)
            {
                return default;
            }
        }

        /// <summary>
        /// ベースラインごとに初期姿勢を求める
        /// これは制約の解決で利用される
        /// AnimationPoseRatioが1.0ならば不要なのでスキップされる
        /// </summary>
        [BurstCompile]
        struct UpdateStepBasicPotureJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepBaseLineIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexLocalRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartDataIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;

            // buffer
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> stepBasicPositionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> stepBasicRotationArray;

            // ステップ実行ベースラインごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// ステップ終了後の座標確定処理
        /// </summary>
        [BurstCompile]
        struct EndSimulationStepJob : IJobParallelForDefer
        {
            public float simulationDeltaTime;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> oldPosArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> velocityArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> staticFrictionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> collisionNormalArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション完了後の表示位置の計算
        /// - 未来予測
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CalcDisplayPosition(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct CalcDisplayPositionJob : IJobParallelFor
        {
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> realVelocityArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> oldRotationArray;
            public NativeArray<float3> dispPosArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> positions;
            //[Unity.Collections.ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotations;

            // すべてのパーティクルごと
            public void Execute(int pindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// tempFloat3Bufferの内容をnextPosArrayに書き戻す
        /// </summary>
        /// <param name="particleList"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle FeedbackTempFloat3Buffer(in NativeList<int> particleList, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct FeedbackTempPosJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> tempFloat3Buffer;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            public void Execute(int index)
            {
            }
        }

        internal JobHandle FeedbackTempFloat3Buffer(in ExProcessingList<int> processingList, JobHandle jobHandle)
        {
            return default;
        }

        unsafe internal JobHandle FeedbackTempFloat3Buffer(in NativeArray<int> particleArray, in NativeReference<int> counter, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct FeedbackTempPosJob2 : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> tempFloat3Buffer;

            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
