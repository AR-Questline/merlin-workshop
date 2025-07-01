// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// コライダーの管理
    /// MC1と違いコライダーはパーティクルとは別に管理される
    /// コライダーはチームごとに分けて管理される。
    /// 同じコライダーをチームAとチームBが共有していたとしてもそれぞれ別のコライダーとして登録される。
    /// </summary>
    public class ColliderManager : IManager, IValid
    {
        /// <summary>
        /// チームID
        /// </summary>
        public ExNativeArray<short> teamIdArray;

        /// <summary>
        /// コライダー種類(最大15まで)
        /// </summary>
        public enum ColliderType : byte
        {
            None = 0,
            Sphere = 1,
            CapsuleX_Center = 2,
            CapsuleY_Center = 3,
            CapsuleZ_Center = 4,
            CapsuleX_Start = 5,
            CapsuleY_Start = 6,
            CapsuleZ_Start = 7,
            Plane = 8,
            Box = 9,
        }

        /// <summary>
        /// フラグ(8bit)
        /// 下位4bitはコライダー種類
        /// 上位4bitはフラグ
        /// </summary>
        public const byte Flag_Valid = 0x10; // データの有無
        public const byte Flag_Enable = 0x20; // 有効状態
        public const byte Flag_Reset = 0x40; // 位置リセット
        public ExNativeArray<ExBitFlag8> flagArray;

        /// <summary>
        /// トランスフォームからの中心ローカルオフセット位置
        /// </summary>
        public ExNativeArray<float3> centerArray;

        /// <summary>
        /// コライダーのサイズ情報
        /// Sphere(x:半径)
        /// Capsule(x:始点半径, y:終点半径, z:長さ)
        /// Box(x:サイズX, y:サイズY, z:サイズZ)
        /// </summary>
        public ExNativeArray<float3> sizeArray;

        /// <summary>
        /// 現フレーム姿勢
        /// トランスフォームからスナップされたチームローカル姿勢
        /// センターオフセットも計算される
        /// </summary>
        public ExNativeArray<float3> framePositions;
        public ExNativeArray<quaternion> frameRotations;
        public ExNativeArray<float3> frameScales;

        /// <summary>
        /// １つ前のフレーム姿勢
        /// </summary>
        public ExNativeArray<float3> oldFramePositions;
        public ExNativeArray<quaternion> oldFrameRotations;
        //public ExNativeArray<float3> oldFrameScales;

        /// <summary>
        /// 現ステップでの姿勢
        /// </summary>
        public ExNativeArray<float3> nowPositions;
        public ExNativeArray<quaternion> nowRotations;
        //public ExNativeArray<float3> nowScales;

        public ExNativeArray<float3> oldPositions;
        public ExNativeArray<quaternion> oldRotations;


        /// <summary>
        /// 有効なコライダーデータ数
        /// </summary>
        public int DataCount => teamIdArray?.Count ?? 0;

        /// <summary>
        /// 登録コライダーコンポーネント
        /// </summary>
        public HashSet<ColliderComponent> colliderSet = new HashSet<ColliderComponent>();

        /// <summary>
        /// 登録コライダー数
        /// </summary>
        public int ColliderCount => colliderSet.Count;

        bool isValid = false;

        //=========================================================================================
        /// <summary>
        /// ステップごとの作業データ
        /// </summary>
        internal struct WorkData
        {
            public AABB aabb;
            public float2 radius;
            public float3x2 oldPos;
            public float3x2 nextPos;
            public quaternion inverseOldRot;
            public quaternion rot;
        }

        internal ExNativeArray<WorkData> workDataArray;

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
        /// チームにコライダー領域を登録する
        /// 最初から最大コライダー数で領域を初期化しておく
        /// </summary>
        /// <param name="cprocess"></param>
        public void Register(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// チームからコライダー領域を解除する
        /// </summary>
        /// <param name="cprocess"></param>
        public void Exit(ClothProcess cprocess)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 初期コライダーの登録
        /// </summary>
        /// <param name="cprocess"></param>
        internal void InitColliders(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// コライダーの内容を更新する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void UpdateColliders(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// コライダーの個別登録
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="col"></param>
        void AddCollider(ClothProcess cprocess, ColliderComponent col)
        {
        }

        /// <summary>
        /// コライダーを削除する
        /// ここでは領域は削除せずにデータのみを無効化させる
        /// 領域は生存する最後尾のデータと入れ替えられる(SwapBack)
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="localIndex"></param>
        internal void RemoveCollider(ColliderComponent col, int teamId)
        {
        }

        void AddColliderInternal(ClothProcess cprocess, ColliderComponent col, int index, int arrayIndex, int transformIndex)
        {
        }

        /// <summary>
        /// 有効状態の変更
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="index"></param>
        /// <param name="sw"></param>
        internal void EnableCollider(ColliderComponent col, int teamId, bool sw)
        {
        }

        /// <summary>
        /// チーム有効状態変更に伴うコライダー状態の変更
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        internal void EnableTeamCollider(int teamId)
        {
        }

        /// <summary>
        /// コライダーコンポーネントのパラメータ変更を反映する
        /// </summary>
        /// <param name="col"></param>
        /// <param name="teamId"></param>
        internal void UpdateParameters(ColliderComponent col, int teamId)
        {
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション更新前処理
        /// コライダー姿勢の読み取り
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
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            //[Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> centerArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> framePositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> frameRotations;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> frameScales;
            public NativeArray<float3> oldFramePositions;
            public NativeArray<quaternion> oldFrameRotations;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<float3> oldFrameScales;
            public NativeArray<float3> nowPositions;
            public NativeArray<quaternion> nowRotations;
            public NativeArray<float3> oldPositions;
            public NativeArray<quaternion> oldRotations;

            // transform (ワールド姿勢)
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // コライダーごと
            public void Execute(int index)
            {
            }
        }


        /// <summary>
        /// 今回のシミュレーションステップで計算が必要なコライダーリストを作成する
        /// </summary>
        /// <param name="updateIndex"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CreateUpdateColliderList(int updateIndex, JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct CreateUpdatecolliderListJob : IJobParallelFor
        {
            public int updateIndex;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> jobColliderCounter;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> jobColliderIndexList;

            public void Execute(int teamId)
            {
            }
        }

        /// <summary>
        /// シミュレーションステップ前処理
        /// コライダーの更新および作業データ作成
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle StartSimulationStep(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct StartSimulationStepJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobColliderIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> sizeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> framePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> frameRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> frameScales;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldFramePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldFrameRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> nowPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> nowRotations;
            [NativeDisableParallelForRestriction]
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPositions;
            [NativeDisableParallelForRestriction]
            //[Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<WorkData> workDataArray;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// シミュレーションステップ後処理
        /// old姿勢の格納
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle EndSimulationStep(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct EndSimulationStepJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobColliderIndexList;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nowPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> nowRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> oldRotations;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// シミュレーション更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostSimulationUpdate(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct PostSimulationUpdateJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> framePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> frameRotations;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> frameScales;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> oldFramePositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> oldFrameRotations;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<float3> oldFrameScales;

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
