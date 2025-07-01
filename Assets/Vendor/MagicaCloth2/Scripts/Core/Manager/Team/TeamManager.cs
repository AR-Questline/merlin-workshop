// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using System;
using System.Collections.Generic;
using System.Text;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    public class TeamManager : IManager, IValid
    {
        /// <summary>
        /// チームフラグ(64bit)
        /// </summary>
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Reset = 2; // 姿勢リセット
        public const int Flag_TimeReset = 3; // 時間リセット
        public const int Flag_Suspend = 4; // 一時停止
        public const int Flag_Running = 5; // 今回のフレームでシミュレーションが実行されたかどうか
        //public const int Flag_CustomSkinning = 6; // カスタムスキニングを使用(未使用)
        //public const int Flag_NormalAdjustment = 9; // 法線調整(未使用)
        public const int Flag_Synchronization = 6; // 同期中
        public const int Flag_StepRunning = 7; // ステップ実行中
        public const int Flag_Exit = 8; // 存在消滅時
        public const int Flag_KeepTeleport = 9; // 姿勢保持テレポート
        public const int Flag_InertiaShift = 10; // 慣性全体シフト
        public const int Flag_CullingInvisible = 11; // カリングによる非表示状態
        public const int Flag_CullingKeep = 12; // カリング時に姿勢を保つ
        public const int Flag_Spring = 13; // Spring利用
        public const int Flag_SkipWriting = 14; // 書き込み停止（ストップモーション用）
        public const int Flag_Anchor = 15; // Inertia anchorを利用中
        public const int Flag_AnchorReset = 16; // Inertia anchorの座標リセット
        public const int Flag_NegativeScale = 17; // マイナススケールの有無
        public const int Flag_NegativeScaleTeleport = 18; // マイナススケールによるテレポート

        // 以下セルフコリジョン
        // !これ以降の順番を変えないこと
        public const int Flag_Self_PointPrimitive = 32; // PointPrimitive+Sortを保持し更新する
        public const int Flag_Self_EdgePrimitive = 33; // EdgePrimitive+Sortを保持し更新する
        public const int Flag_Self_TrianglePrimitive = 34; // TrianglePrimitive+Sortを保持し更新する

        public const int Flag_Self_EdgeEdge = 35;
        public const int Flag_Sync_EdgeEdge = 36;
        public const int Flag_PSync_EdgeEdge = 37;

        public const int Flag_Self_PointTriangle = 38;
        public const int Flag_Sync_PointTriangle = 39;
        public const int Flag_PSync_PointTriangle = 40;

        public const int Flag_Self_TrianglePoint = 41;
        public const int Flag_Sync_TrianglePoint = 42;
        public const int Flag_PSync_TrianglePoint = 43;

        public const int Flag_Self_EdgeTriangleIntersect = 44;
        public const int Flag_Sync_EdgeTriangleIntersect = 45;
        public const int Flag_PSync_EdgeTriangleIntersect = 46;
        public const int Flag_Self_TriangleEdgeIntersect = 47;
        public const int Flag_Sync_TriangleEdgeIntersect = 48;
        public const int Flag_PSync_TriangleEdgeIntersect = 49;

        /// <summary>
        /// チーム基本データ
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// フラグ
            /// </summary>
            public BitField64 flag;

            /// <summary>
            /// 更新モード
            /// </summary>
            public ClothUpdateMode updateMode;

            /// <summary>
            /// １秒間の更新頻度
            /// </summary>
            //public int frequency;

            /// <summary>
            /// 現在フレームの更新時間
            /// </summary>
            public float frameDeltaTime;

            /// <summary>
            /// 更新計算用時間
            /// </summary>
            public float time;

            /// <summary>
            /// 前フレームの更新計算用時間
            /// </summary>
            public float oldTime;

            /// <summary>
            /// 現在のシミュレーション更新時間
            /// </summary>
            public float nowUpdateTime;

            /// <summary>
            /// １つ前の最後のシミュレーション更新時間
            /// </summary>
            public float oldUpdateTime;

            /// <summary>
            /// 更新がある場合のフレーム時間
            /// </summary>
            public float frameUpdateTime;

            /// <summary>
            /// 前回更新のフレーム時間
            /// </summary>
            public float frameOldTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// チームの最終計算用タイムスケール(0.0~1.0)
            /// グローバルタイムスケールなどを考慮した値
            /// </summary>
            public float nowTimeScale;

            /// <summary>
            /// 今回のチーム更新回数（０ならばこのフレームは更新なし）
            /// </summary>
            public int updateCount;

            /// <summary>
            /// 今回のチーム更新スキップ回数（１以上ならばシミュレーションスキップが発生）
            /// </summary>
            public int skipCount;

            /// <summary>
            /// ステップごとのフレームに対するnowUpdateTime割合
            /// これは(frameStartTime ~ time)間でのnowUpdateTimeの割合
            /// </summary>
            public float frameInterpolation;

            /// <summary>
            /// 重力の影響力(0.0 ~ 1.0)
            /// 1.0は重力が100%影響する
            /// </summary>
            public float gravityRatio;

            public float gravityDot;

            /// <summary>
            /// センタートランスフォーム(ダイレクト値)
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現在の中心ワールド座標（この値はCenterData.nowWorldPositionのコピー）
            /// </summary>
            //public float3 centerWorldPosition;

            /// <summary>
            /// アンカーとして設定されているTransformのインスタンスID(0=なし)
            /// </summary>
            public int anchorTransformId;

            /// <summary>
            /// チームスケール
            /// </summary>
            public float3 initScale;            // データ生成時のセンタートランスフォームスケール
            public float scaleRatio;            // 現在のスケール倍率

            /// <summary>
            /// マイナススケール
            /// </summary>
            public float negativeScaleSign;             // マイナススケールの有無(1:正スケール, -1:マイナススケール)
            public float3 negativeScaleDirection;       // スケール方向(xyz)：(1:正スケール, -1:マイナススケール)
            public float3 negativeScaleChange;          // 今回のフレームで変化したスケール(xyz)：(1:変化なし, -1:反転した)
            public float2 negativeScaleTriangleSign;    // トライアングル法線接線フリップフラグ
            public float4 negativeScaleQuaternionValue; // クォータニオン反転用

            /// <summary>
            /// 同期チームID(0=なし)
            /// </summary>
            public int syncTeamId;

            /// <summary>
            /// 自身を同期している親チームID(0=なし)：最大７つ
            /// </summary>
            public FixedList32Bytes<int> syncParentTeamId;

            /// <summary>
            /// 同期先チームのセンタートランスフォームインデックス（ダイレクト値）
            /// </summary>
            public int syncCenterTransformIndex;

            /// <summary>
            /// 初期姿勢とアニメーション姿勢のブレンド率（制約で利用）
            /// </summary>
            public float animationPoseRatio;

            /// <summary>
            /// 速度安定時間(StablizationTime)による速度適用割合(0.0 ~ 1.0)
            /// </summary>
            public float velocityWeight;

            /// <summary>
            /// シミュレーション結果ブレンド割合(0.0 ~ 1.0)
            /// </summary>
            public float blendWeight;

            /// <summary>
            /// 外力モード
            /// </summary>
            public ClothForceMode forceMode;

            /// <summary>
            /// 外力
            /// </summary>
            public float3 impactForce;

            //-----------------------------------------------------------------
            /// <summary>
            /// ProxyMeshのタイプ
            /// </summary>
            public VirtualMesh.MeshType proxyMeshType;

            /// <summary>
            /// ProxyMeshのTransformデータ
            /// </summary>
            public DataChunk proxyTransformChunk;

            /// <summary>
            /// ProxyMeshの共通部分
            /// -attributes
            /// -vertexToTriangles
            /// -vertexToVertexIndexArray
            /// -vertexDepths
            /// -vertexLocalPositions
            /// -vertexLocalRotations
            /// -vertexRootIndices
            /// -vertexParentIndices
            /// -vertexChildIndexArray
            /// -vertexAngleCalcLocalRotations
            /// -uv
            /// -positions
            /// -rotations
            /// -vertexBindPosePositions
            /// -vertexBindPoseRotations
            /// -normalAdjustmentRotations
            /// </summary>
            public DataChunk proxyCommonChunk;

            /// <summary>
            /// ProxyMeshの頂点接続頂点データ
            /// -vertexToVertexDataArray (-vertexToVertexIndexArrayと対)
            /// </summary>
            //public DataChunk proxyVertexToVertexDataChunk;

            /// <summary>
            /// ProxyMeshの子頂点データ
            /// -vertexChildDataArray (-vertexChildIndexArrayと対)
            /// </summary>
            public DataChunk proxyVertexChildDataChunk;

            /// <summary>
            /// ProxyMeshのTriangle部分
            /// -triangles
            /// -triangleTeamIdArray
            /// -triangleNormals
            /// -triangleTangents
            /// </summary>
            public DataChunk proxyTriangleChunk;

            /// <summary>
            /// ProxyMeshのEdge部分
            /// -edges
            /// -edgeTeamIdArray
            /// </summary>
            public DataChunk proxyEdgeChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth/MeshCloth共通部分
            /// -localPositions
            /// -localNormals
            /// -localTangents
            /// -boneWeights
            /// </summary>
            public DataChunk proxyMeshChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth固有部分
            /// -vertexToTransformRotations
            /// </summary>
            public DataChunk proxyBoneChunk;

            /// <summary>
            /// ProxyMeshのMeshClothのスキニングボーン部分
            /// -skinBoneTransformIndices
            /// -skinBoneBindPoses
            /// </summary>
            public DataChunk proxySkinBoneChunk;

            /// <summary>
            /// ProxyMeshのベースライン部分
            /// -baseLineFlags
            /// -baseLineStartDataIndices
            /// -baseLineDataCounts
            /// </summary>
            public DataChunk baseLineChunk;

            /// <summary>
            /// ProxyMeshのベースラインデータ配列
            /// -baseLineData
            /// </summary>
            public DataChunk baseLineDataChunk;

            /// <summary>
            /// 固定点リスト
            /// </summary>
            public DataChunk fixedDataChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// 接続しているマッピングメッシュへデータへのインデックスセット(最大15まで)
            /// </summary>
            //public FixedList32Bytes<short> mappingDataIndexSet;

            //-----------------------------------------------------------------
            /// <summary>
            /// パーティクルデータ
            /// </summary>
            public DataChunk particleChunk;

            /// <summary>
            /// コライダーデータ
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderChunk;

            /// <summary>
            /// コライダートランスフォーム
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderTransformChunk;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int colliderCount;

            //-----------------------------------------------------------------
            /// <summary>
            /// 距離制約
            /// </summary>
            public DataChunk distanceStartChunk;
            public DataChunk distanceDataChunk;

            /// <summary>
            /// 曲げ制約
            /// </summary>
            public DataChunk bendingPairChunk;
            //public DataChunk bendingDataChunk;
            public DataChunk bendingWriteIndexChunk;
            public DataChunk bendingBufferChunk;

            /// <summary>
            /// セルフコリジョン制約
            /// </summary>
            //public int selfQueueIndex;
            public DataChunk selfPointChunk;
            public DataChunk selfEdgeChunk;
            public DataChunk selfTriangleChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// UnityPhysicsでの更新の必要性
            /// </summary>
            public bool IsFixedUpdate => updateMode == ClothUpdateMode.UnityPhysics;

            /// <summary>
            /// タイムスケールを無視
            /// </summary>
            public bool IsUnscaled => updateMode == ClothUpdateMode.Unscaled;

            /// <summary>
            /// １回の更新間隔
            /// </summary>
            //public float SimulationDeltaTime => 1.0f / frequency;

            /// <summary>
            /// データの有効性
            /// </summary>
            public bool IsValid => flag.IsSet(Flag_Valid);

            /// <summary>
            /// 有効状態
            /// </summary>
            public bool IsEnable => flag.IsSet(Flag_Enable);

            /// <summary>
            /// 処理状態
            /// </summary>
            public bool IsProcess => flag.IsSet(Flag_Enable) && flag.IsSet(Flag_Suspend) == false && flag.IsSet(Flag_CullingInvisible) == false;

            /// <summary>
            /// 姿勢リセット有無
            /// </summary>
            public bool IsReset => flag.IsSet(Flag_Reset);

            /// <summary>
            /// 姿勢維持テレポートの有無
            /// </summary>
            public bool IsKeepReset => flag.IsSet(Flag_KeepTeleport);

            /// <summary>
            /// 慣性全体シフトの有無
            /// </summary>
            public bool IsInertiaShift => flag.IsSet(Flag_InertiaShift);

            /// <summary>
            /// 今回のフレームでシミュレーションが実行されたかどうか（１回以上実行された場合）
            /// </summary>
            public bool IsRunning => flag.IsSet(Flag_Running);

            /// <summary>
            /// ステップ実行中かどうか
            /// </summary>
            public bool IsStepRunning => flag.IsSet(Flag_StepRunning);

            public bool IsCullingInvisible => flag.IsSet(Flag_CullingInvisible);
            public bool IsCullingKeep => flag.IsSet(Flag_CullingKeep);
            public bool IsSpring => flag.IsSet(Flag_Spring);
            public bool IsNegativeScale => flag.IsSet(Flag_NegativeScale);
            public bool IsNegativeScaleTeleport => flag.IsSet(Flag_NegativeScaleTeleport);
            public int ParticleCount => particleChunk.dataLength;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int ColliderCount => colliderCount;
            public int BaseLineCount => baseLineChunk.dataLength;
            public int TriangleCount => proxyTriangleChunk.dataLength;
            public int EdgeCount => proxyEdgeChunk.dataLength;

            //public int MappingCount => mappingDataIndexSet.Length;

            /// <summary>
            /// 初期スケール（ｘ軸のみで判定、均等スケールしか認めていない）
            /// </summary>
            public float InitScale => initScale.x;
        }
        
        public ExNativeArray<TeamData> teamDataArray;
        UnsafeInfiniteBitmask _desiredEnableTeamMask;
        UnsafeInfiniteBitmask _enableTeamMaskToChange;

        /// <summary>
        /// チームごとの風の影響情報
        /// </summary>
        public ExNativeArray<TeamWindData> teamWindArray;

        /// <summary>
        /// マッピングメッシュデータ
        /// </summary>
        public struct MappingData : IValid
        {
            public int teamId;

            /// <summary>
            /// Mappingメッシュのセンタートランスフォーム（ダイレクト値）
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// Mappingメッシュの基本
            /// -attributes
            /// -localPositions
            /// -localNormlas
            /// -localTangents
            /// -boneWeights
            /// -positions
            /// -rotations
            /// </summary>
            public DataChunk mappingCommonChunk;

            /// <summary>
            /// 初期状態でのプロキシメッシュへの変換マトリックスと変換回転
            /// この姿勢は初期化時に固定される
            /// </summary>
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            /// <summary>
            /// プロキシメッシュとマッピングメッシュの座標空間が同じかどうか
            /// </summary>
            public bool sameSpace;

            /// <summary>
            /// プロキシメッシュからマッピングメッシュへの座標空間変換用
            /// ▲ワールド対応：ここはワールド空間からマッピングメッシュへの座標変換となる
            /// </summary>
            public float4x4 toMappingMatrix;
            public quaternion toMappingRotation;

            /// <summary>
            /// Mappingメッシュ用のスケーリング比率
            /// </summary>
            public float scaleRatio;

            public bool IsValid()
            {
                return default;
            }

            public int VertexCount => mappingCommonChunk.dataLength;
        }
        public ExNativeArray<MappingData> mappingDataArray;

        /// <summary>
        /// チームごとのマッピングメッシュIDリスト（チームごとに最大31まで)
        /// </summary>
        public ExNativeArray<FixedList64Bytes<short>> teamMappingIndexArray;

        /// <summary>
        /// チーム全体の最大更新回数
        /// </summary>
        public NativeReference<int> maxUpdateCount;

        /// <summary>
        /// パラメータ（teamDataArrayとインデックス連動）
        /// </summary>
        public ExNativeArray<ClothParameters> parameterArray;

        /// <summary>
        /// センタートランスフォームデータ
        /// </summary>
        public ExNativeArray<InertiaConstraint.CenterData> centerDataArray;

        /// <summary>
        /// 登録されているマッピングメッシュ数
        /// </summary>
        public int MappingCount => mappingDataArray?.Count ?? 0;

        /// <summary>
        /// チームIDとClothProcessクラスの関連辞書
        /// </summary>
        Dictionary<int, ClothProcess> clothProcessDict = new Dictionary<int, ClothProcess>();

        //=========================================================================================
        bool isValid;

        /// <summary>
        /// グローバルタイムスケール(0.0 ~ 1.0)
        /// </summary>
        //internal float globalTimeScale = 1.0f;

        /// <summary>
        /// フレームのFixedUpdate回数
        /// </summary>
        //int fixedUpdateCount = 0;

        /// <summary>
        /// エッジコライダーコリジョンのエッジ数合計
        /// </summary>
        internal int edgeColliderCollisionCount;

        //=========================================================================================
        /// <summary>
        /// 登録されているチーム数（グローバルチームを含む。そのため０にはならない）
        /// </summary>
        public int TeamCount => teamDataArray?.Count ?? 0;

        /// <summary>
        /// 登録されている有効なチーム数（グローバルチームを含まない）
        /// </summary>
        public int TrueTeamCount => clothProcessDict.Count;

        /// <summary>
        /// 実行状態にあるチーム数
        /// </summary>
        public int ActiveTeamCount => (int)_desiredEnableTeamMask.CountOnes();

        public bool HasAny => _desiredEnableTeamMask.AnySet();

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
        /// チームを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="clothParams"></param>
        /// <returns></returns>
        internal int AddTeam(ClothProcess cprocess, ClothParameters clothParams)
        {
            return default;
        }

        /// <summary>
        /// チームを解除する
        /// </summary>
        /// <param name="teamId"></param>
        internal void RemoveTeam(int teamId)
        {
        }

        /// <summary>
        /// チームの有効化設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
        }

        void SetEnableImpl(int teamId, bool sw)
        {
        }

        public void UpdateCachedEnables()
        {
        }

        public bool IsEnable(int teamId)
        {
            return default;
        }

        internal void SetSkipWriting(int teamId, bool sw)
        {
        }

        public bool ContainsTeamData(int teamId)
        {
            return default;
        }

        public ref TeamData GetTeamDataRef(int teamId)
        {
            throw new NotImplementedException();
        }

        public ref FixedList64Bytes<short> GetTeamMappingRef(int teamId)
        {
            throw new NotImplementedException();
        }

        public ref ClothParameters GetParametersRef(int teamId)
        {
            throw new NotImplementedException();
        }

        internal ref InertiaConstraint.CenterData GetCenterDataRef(int teamId)
        {
            throw new NotImplementedException();
        }

        public ClothProcess GetClothProcess(int teamId)
        {
            return default;
        }

        //=========================================================================================
        static readonly ProfilerMarker teamUpdateCullingProfiler = new ProfilerMarker("TeamUpdateCulling");

        /// <summary>
        /// カリング状態更新
        /// </summary>
        internal void TeamCullingUpdate()
        {
        }

        //=========================================================================================
        /// <summary>
        /// チーム更新後処理用作業リスト
        /// </summary>
        List<ClothProcess> workPostProcessList = new List<ClothProcess>(256);

        /// <summary>
        /// 毎フレーム常に実行するチーム更新
        /// - 時間の更新と実行回数の算出
        /// </summary>
        internal void AlwaysTeamUpdate()
        {
        }

        [BurstCompile]
        struct AlwaysTeamUpdateJob : IJob
        {
            public int teamCount;
            public float unityFrameDeltaTime;
            public float unityFrameFixedDeltaTime;
            public float unityFrameUnscaledDeltaTime;
            public float globalTimeScale;
            public float simulationDeltaTime;
            //public float maxDeltaTime;
            public int maxSimmulationCountPerFrame;

            public NativeReference<int> maxUpdateCount;
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            public void Execute()
            {
            }
        }

        bool AddSyncParent(ref TeamData tdata, int parentTeamId)
        {
            return default;
        }

        void RemoveSyncParent(ref TeamData tdata, int parentTeamId)
        {
        }

        //=========================================================================================
        /// <summary>
        /// チームごとのセンター姿勢の決定と慣性用の移動量計算
        /// および風の影響を計算
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CalcCenterAndInertiaAndWind(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct CalcCenterAndInertiaAndWindJob : IJobParallelFor
        {
            public float simulationDeltaTime;

            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            public NativeArray<TeamWindData> teamWindArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;

            // inertia
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> fixedArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // wind
            public int windZoneCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // チームごと
            public void Execute(int teamId)
            {
            }

            /// <summary>
            /// チームが受ける風ゾーンのリストを作成する
            /// ゾーンが追加タイプでない場合はチームが接触する最も体積が小さいゾーンが１つ有効になる。
            /// ゾーンが追加タイプの場合は最大３つまでが有効になる。
            /// </summary>
            /// <param name="teamId"></param>
            /// <param name="param"></param>
            /// <param name="centerWorldPos"></param>
            void Wind(int teamId, in ClothParameters param, in float3 centerWorldPos)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// ステップごとの前処理（ステップの開始に実行される）
        /// </summary>
        /// <param name="updateIndex"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle SimulationStepTeamUpdate(int updateIndex, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct SimulationStepTeamUpdateJob : IJobParallelFor
        {
            public int updateIndex;
            public float simulationDeltaTime;

            // team
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            public NativeArray<TeamWindData> teamWindArray;

            // チームごと
            public void Execute(int teamId)
            {
            }

            // 各風ゾーンの時間更新
            void UpdateWind(int teamId, in TeamData tdata, in WindParams windParams, in InertiaConstraint.CenterData cdata)
            {
            }

            void UpdateWindTime(ref TeamWindInfo windInfo, float frequency, float simulationDeltaTime)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーション更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostTeamUpdate(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct PostTeamUpdateJob : IJobParallelFor
        {
            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // チームごと
            public void Execute(int teamId)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// ここに登録されるのはClothコンポーネントがDisable時に初期化されたプロセス
        /// これらのプロセスはマネージャ側で消滅が監視される
        /// </summary>
        HashSet<ClothProcess> monitoringProcessSet = new HashSet<ClothProcess>();
        List<ClothProcess> disposeProcessList = new List<ClothProcess>();

        internal void AddMonitoringProcess(ClothProcess cprocess)
        {
        }

        internal void RemoveMonitoringProcess(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// コンポーネントDisable時に初期化されたClothProcessを監視し消滅していたらマネージャ側からメモリを開放する
        /// </summary>
        /// <param name="force"></param>
        void MonitoringProcess(bool force)
        {
        }

        void MonitoringProcessUpdate() => MonitoringProcess(false);

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
