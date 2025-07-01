// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class SelfCollisionConstraint : IDisposable
    {
        public enum SelfCollisionMode
        {
            None = 0,

            /// <summary>
            /// PointPoint
            /// </summary>
            //Point = 1, // omit!

            /// <summary>
            /// PointTriangle + EdgeEdge + Intersect
            /// </summary>
            FullMesh = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// self-collision mode
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode selfMode;

            /// <summary>
            /// primitive thickness.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData surfaceThickness = new CurveSerializeData(0.005f, 0.5f, 1.0f, false);

            /// <summary>
            /// mutual collision mode.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode syncMode;

            /// <summary>
            /// Mutual Collision Opponent.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public MagicaCloth syncPartner;

            /// <summary>
            /// cloth weight.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float clothMass = 0.0f;

            public SerializeData()
            {
            }

            public void DataValidate()
            {
            }

            public SerializeData Clone()
            {
                return default;
            }

            public MagicaCloth GetSyncPartner()
            {
                return default;
            }
        }

        public struct SelfCollisionConstraintParams
        {
            public SelfCollisionMode selfMode;
            public float4x4 surfaceThicknessCurveData;
            public SelfCollisionMode syncMode;
            public float clothMass;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
            }
        }

        //=========================================================================================
        public const uint KindPoint = 0;
        public const uint KindEdge = 1;
        public const uint KindTriangle = 2;

        public const uint Flag_KindMask = 0x03000000; // 24~25bit
        public const uint Flag_Fix0 = 0x04000000;
        public const uint Flag_Fix1 = 0x08000000;
        public const uint Flag_Fix2 = 0x10000000;
        public const uint Flag_AllFix = 0x20000000;
        public const uint Flag_Ignore = 0x40000000; // 無効もしくは無視頂点が含まれる
        public const uint Flag_Enable = 0x80000000; // 接触判定有効

        struct Primitive
        {
            /// <summary>
            /// フラグとチームID
            /// 上位8bit = フラグ
            /// 下位24bit = チームID
            /// </summary>
            public uint flagAndTeamId;

            /// <summary>
            /// ソートリストへのインデックス（グローバル）
            /// </summary>
            public int sortIndex;

            /// <summary>
            /// プリミティグを構成するパーティクルインデックス
            /// </summary>
            public int3 particleIndices;

            public float3x3 nextPos;
            public float3x3 oldPos;
            //public float3x3 basePos;
            public float3 invMass;

            /// <summary>
            /// 厚み
            /// </summary>
            public float thickness;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsIgnore()
            {
                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasParticle(int p)
            {
                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetKind()
            {
                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetTeamId()
            {
                return default;
            }

            /// <summary>
            /// 解決時のthicknessを計算する
            /// </summary>
            /// <param name="pri"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetSolveThickness(in Primitive pri)
            {
                return default;
            }

            /// <summary>
            /// パーティクルインデックスが１つ以上重複しているか判定する
            /// </summary>
            /// <param name="pri"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnyParticle(in Primitive pri)
            {
                return default;
            }
        }
        ExNativeArray<Primitive> primitiveArray;

        struct SortData : IComparable<SortData>
        {
            /// <summary>
            /// フラグとチームID
            /// 上位8bit = フラグ
            /// 下位24bit = チームID
            /// </summary>
            public uint flagAndTeamId;

            /// <summary>
            /// プリミティブインデックス（グローバル）
            /// </summary>
            public int primitiveIndex;

            public float2 firstMinMax;
            public float2 secondMinMax;
            public float2 thirdMinMax;

            public int CompareTo(SortData other)
            {
                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetKind()
            {
                return default;
            }
        }
        ExNativeArray<SortData> sortAndSweepArray;

        /// <summary>
        /// ポイントプリミティブ総数
        /// </summary>
        public int PointPrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// エッジプリミティブ総数
        /// </summary>
        public int EdgePrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// トライアングルプリミティブ総数
        /// </summary>
        public int TrianglePrimitiveCount { get; private set; } = 0;

        //=========================================================================================
        internal struct EdgeEdgeContact
        {
            public uint flagAndTeamId0;
            public uint flagAndTeamId1;
            public half thickness;
            public half s;
            public half t;
            public half3 n;
            public half2 edgeInvMass0;
            public half2 edgeInvMass1;
            public int2 edgeParticleIndex0;
            public int2 edgeParticleIndex1;

            public override string ToString()
            {
                return default;
            }
        }
        NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;
        NativeList<EdgeEdgeContact> edgeEdgeContactList;

        internal struct PointTriangleContact
        {
            public uint flagAndTeamId0; // point
            public uint flagAndTeamId1; // triangle
            public half thickness;
            public half sign; // 押出方向(-1/+1)
            public int pointParticleIndex;
            public int3 triangleParticleIndex;
            public half pointInvMass;
            public half3 triangleInvMass;

            public override string ToString()
            {
                return default;
            }
        }
        NativeQueue<PointTriangleContact> pointTriangleContactQueue;
        NativeList<PointTriangleContact> pointTriangleContactList;

        /// <summary>
        /// 交差解決フラグ(パーティクルと連動)
        /// </summary>
        NativeArray<byte> intersectFlagArray;

        public int IntersectCount { get; private set; } = 0;

        //=========================================================================================
        public SelfCollisionConstraint()
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// データの有無を返す
        /// </summary>
        /// <returns></returns>
        public bool HasPrimitive()
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
#if false
        internal class ConstraintData : IValid
        {
            public ResultCode result;

            /// <summary>
            /// 同期先proxyMeshのlocalPosを自proxyMesh空間に変換するマトリックス
            /// </summary>
            public float4x4 syncToSelfMatrix;

            public bool IsValid()
            {
                return math.any(syncToSelfMatrix.c0);
            }
        }

        internal static ConstraintData CreateData(
            int teamId, TeamManager.TeamData teamData, VirtualMesh proxyMesh, in ClothParameters parameters,
            int syncTeamId, TeamManager.TeamData syncTeamData, VirtualMesh syncProxyMesh)
        {
            var constraintData = new ConstraintData();

            try
            {
                if (proxyMesh.VertexCount == 0)
                    return null;

                var self2Params = parameters.selfCollisionConstraint2;

                // 同期チームとのFullMesh判定が必要な場合は、同期ProxyMeshのローカル頂点を時チームの座標空間に変換しておく
                //var syncMode = parameters.selfCollisionConstraint.syncMode;
                //if (syncTeamId > 0 && syncProxyMesh != null)
                //{
                //    // 同期proxyMeshを自proxyMesh空間に変換するマトリックス
                //    var toM = syncProxyMesh.CenterTransformTo(proxyMesh);
                //    constraintData.syncToSelfMatrix = toM;
                //}
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                constraintData.result.SetError(Define.Result.Constraint_CreateSelfCollisionException);
            }
            finally
            {
            }

            return constraintData;
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// フラグおよびバッファの更新
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="tdata"></param>
        internal void UpdateTeam(int teamId)
        {
        }

        void InitPrimitive(int teamId, TeamManager.TeamData tdata, uint kind, int startPrimitive, int startSort, int length)
        {
        }

        [BurstCompile]
        struct InitPrimitiveJob : IJobParallelFor
        {
            public int teamId;
            public TeamManager.TeamData tdata;

            public uint kind;
            public int startPrimitive;
            public int startSort;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortArray;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 作業バッファ更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
        }

        /// <summary>
        /// ソート＆スイープ配列をデータsdで二分探索しその開始インデックスを返す
        /// </summary>
        /// <param name="sortAndSweepArray"></param>
        /// <param name="sd"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        static unsafe int BinarySearchSortAndlSweep(ref NativeArray<SortData> sortAndSweepArray, in SortData sd, in DataChunk chunk)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle SolverConstraint(int updateIndex, JobHandle jobHandle)
        {
            return default;
        }

        /// <summary>
        /// 実行時セルフコリジョンの解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolverRuntimeSelfCollision(int updateIndex, JobHandle jobHandle)
        {
            return default;
        }

        /// <summary>
        /// コンタクトバッファ生成
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolverBroadPhase(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct ClearBufferJob : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;

            [Unity.Collections.WriteOnly]
            public NativeQueue<PointTriangleContact> pointTriangleContactQueue;

            public void Execute()
            {
            }
        }

#if false
        [BurstCompile]
        struct SetupContactGroupJob : IJob
        {
            public int teamCount;
            public int useQueueCount;

            // team
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute()
            {
                // まずセルフコリジョンが有効で同期していないチームのグループインデックスを決定する
                int groupIndex = 0;
                var restTeam = new NativeList<int>(teamCount, Allocator.Temp);
                for (int i = 1; i < teamCount; i++)
                {
                    var tdata = teamDataArray[i];
                    if (tdata.IsValid == false || tdata.IsEnable == false || tdata.IsStepRunning == false)
                        continue;
                    if (tdata.flag.TestAny(TeamManager.Flag_Self_PointPrimitive, 3) == false)
                        continue;

                    // このチームはセルフコリジョンを実行する
                    if (tdata.flag.IsSet(TeamManager.Flag_Synchronization))
                    {
                        // 同期
                        // 後で解決する
                        restTeam.Add(i);
                    }
                    else
                    {
                        // コンタクトグループを割り振る
                        tdata.selfQueueIndex = groupIndex % useQueueCount;
                        //Debug.Log($"tid:{i} Main selfQueueIndex:{tdata.selfQueueIndex}");
                        teamDataArray[i] = tdata;
                        groupIndex++;
                    }
                }

                // 同期チームは同期先のグループIDを指す
                if (restTeam.Length > 0)
                {
                    foreach (var teamId in restTeam)
                    {
                        var tdata = teamDataArray[teamId];
                        var stdata = teamDataArray[tdata.syncTeamId];
                        while (stdata.syncTeamId != 0)
                        {
                            stdata = teamDataArray[stdata.syncTeamId];
                        }
                        tdata.selfQueueIndex = stdata.selfQueueIndex;
                        teamDataArray[teamId] = tdata;
                        //Debug.Log($"tid:{teamId} Sync selfQueueIndex:{tdata.selfQueueIndex}");
                    }
                }
            }
        }
#endif

        [BurstCompile]
        struct UpdatePrimitiveJob : IJobParallelForDefer
        {
            // プリミティブ種類
            public uint kind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> stepBasicPositionBuffer;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingArray;

            // プリミティブごと
            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        unsafe struct SortJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortAndSweepArray;

            // チームごと(Point/Edge/Triangle)
            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        struct PointTriangleBroadPhaseJob : IJobParallelForDefer
        {
            public uint mainKind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingPointTriangleArray;

            // contact buffer
            [Unity.Collections.WriteOnly]
            public NativeQueue<PointTriangleContact>.ParallelWriter pointTriangleContactQueue;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> intersectFlagArray;

            // 解決PointTriangleごと
            public void Execute(int index)
            {
            }

            void SweepTest(int sortIndex, ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
            }

            void BroadPointTriangle(ref Primitive p_pri, ref Primitive t_pri, float thickness, float scr, float ang)
            {
            }
        }

        [BurstCompile]
        struct EdgeEdgeBroadPhaseJob : IJobParallelForDefer
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingEdgeEdgeArray;

            // contact buffer
            [Unity.Collections.WriteOnly]
            public NativeQueue<EdgeEdgeContact>.ParallelWriter edgeEdgeContactQueue;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> intersectFlagArray;

            // 解決エッジごと
            public void Execute(int index)
            {
            }

            void SweepTest(int sortIndex, ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
            }

            void BroadEdgeEdge(ref Primitive pri0, ref Primitive pri1, float thickness, float scr)
            {
            }
        }

        [BurstCompile]
        struct EdgeEdgeToListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<EdgeEdgeContact> edgeEdgeContactList;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct PointTriangleToListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<PointTriangleContact> pointTriangleContactQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<PointTriangleContact> pointTriangleContactList;

            public void Execute()
            {
            }
        }

        JobHandle UpdateBroadPhase(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct UpdateEdgeEdgeBroadPhaseJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;

            [NativeDisableParallelForRestriction]
            public NativeList<EdgeEdgeContact> edgeEdgeContactList;

            // コンタクトごと
            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        struct UpdatePointTriangleBroadPhaseJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;

            [NativeDisableParallelForRestriction]
            public NativeList<PointTriangleContact> pointTriangleContactList;

            // コンタクトごと
            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        unsafe struct SolverEdgeEdgeJob : IJobParallelForDefer
        {
            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // contact
            [Unity.Collections.ReadOnly]
            public NativeArray<EdgeEdgeContact> edgeEdgeContactArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // コンタクトごと
            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        unsafe struct SolverPointTriangleJob : IJobParallelForDefer
        {
            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // contact
            [Unity.Collections.ReadOnly]
            public NativeArray<PointTriangleContact> pointTriangleContactArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // コンタクトごと
            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 交差（絡まり）の解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolveIntersect(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct IntersectUpdatePrimitiveJob : IJobParallelForDefer
        {
            // プリミティブ種類
            public uint kind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingArray;

            public void Execute(int index)
            {
            }
        }

        [BurstCompile]
        struct IntersectEdgeTriangleJob : IJobParallelForDefer
        {
            public uint mainKind;
            public int execNumber;
            public int div;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingEdgeEdgeArray;

            // out
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> intersectFlagArray;

            // 解決Edge/Triangleごと
            public void Execute(int index)
            {
            }

            void SweepTest(ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
            }

            void IntersectTest(ref Primitive epri, ref Primitive tpri)
            {
            }
        }
    }
}
