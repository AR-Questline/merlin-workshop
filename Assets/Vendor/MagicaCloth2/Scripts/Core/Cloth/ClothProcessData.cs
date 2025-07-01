// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネント処理のデータ部分
    /// </summary>
    public partial class ClothProcess : IDisposable, IValid, ITransform
    {
        public MagicaCloth cloth { get; internal set; }

        /// <summary>
        /// 同期中クロス
        /// </summary>
        public MagicaCloth SyncCloth { get; internal set; }

        /// <summary>
        /// 状態フラグ(0 ~ 31)
        /// </summary>
        public const int State_Valid = 0;
        public const int State_Enable = 1;
        public const int State_ParameterDirty = 2;
        public const int State_InitSuccess = 3;
        public const int State_InitComplete = 4;
        public const int State_Build = 5;
        public const int State_Running = 6;
        public const int State_DisableAutoBuild = 7;
        public const int State_CullingInvisible = 8; // チームデータの同フラグのコピー
        public const int State_CullingKeep = 9; // チームデータの同フラグのコピー
        public const int State_SkipWriting = 10; // 書き込み停止（ストップモーション用）
        public const int State_SkipWritingDirty = 11; // 書き込み停止フラグ更新サイン
        public const int State_UsePreBuild = 12; // PreBuildを利用

        /// <summary>
        /// 現在の状態
        /// </summary>
        internal BitField32 stateFlag;

        /// <summary>
        /// 初期クロスコンポーネントトランスフォーム状態
        /// </summary>
        internal TransformRecord clothTransformRecord { get; private set; } = null;

        /// <summary>
        /// レンダー情報へのハンドル
        /// （レンダラーのセットアップデータ）
        /// </summary>
        List<int> renderHandleList = new List<int>();

        /// <summary>
        /// BoneClothのセットアップデータ
        /// </summary>
        internal RenderSetupData boneClothSetupData;

        /// <summary>
        /// レンダーメッシュの管理
        /// </summary>
        public class RenderMeshInfo
        {
            public int renderHandle;
            public VirtualMeshContainer renderMeshContainer;
            public DataChunk mappingChunk;
        }
        internal List<RenderMeshInfo> renderMeshInfoList = new List<RenderMeshInfo>();

        /// <summary>
        /// カスタムスキニングのボーン情報
        /// </summary>
        internal List<TransformRecord> customSkinningBoneRecords = new List<TransformRecord>();

        /// <summary>
        /// 法線調整用のトランスフォーム状態
        /// </summary>
        internal TransformRecord normalAdjustmentTransformRecord { get; private set; } = null;

        //=========================================================================================
        /// <summary>
        /// ペイントマップ情報
        /// </summary>
        public class PaintMapData
        {
            public const byte ReadFlag_Fixed = 0x01;
            public const byte ReadFlag_Move = 0x02;
            public const byte ReadFlag_Limit = 0x04;

            public Color32[] paintData;
            public int paintMapWidth;
            public int paintMapHeight;
            public ExBitFlag8 paintReadFlag;
        }

        //=========================================================================================
        /// <summary>
        /// 処理結果
        /// </summary>
        internal ResultCode result;
        public ResultCode Result => result;

        /// <summary>
        /// Cloth Type
        /// </summary>
        public enum ClothType
        {
            MeshCloth = 0,
            BoneCloth = 1,
            BoneSpring = 10,
        }
        internal ClothType clothType { get; private set; }

        /// <summary>
        /// リダクション設定（外部から設定する）
        /// </summary>
        ReductionSettings reductionSettings;

        /// <summary>
        /// シミュレーションパラメータ
        /// </summary>
        public ClothParameters parameters { get; private set; }

        /// <summary>
        /// プロキシメッシュ
        /// </summary>
        public VirtualMeshContainer ProxyMeshContainer { get; private set; } = null;

        /// <summary>
        /// コライダーリスト
        /// コライダーが格納されるインデックスは他のデータのインデックスと一致している
        /// </summary>
        internal List<ColliderComponent> colliderList = new List<ColliderComponent>();

        /// <summary>
        /// コライダー配列数
        /// </summary>
        internal int ColliderCapacity => colliderList.Count;

        //=========================================================================================
        /// <summary>
        /// チームID
        /// </summary>
        public int TeamId { get; private set; } = 0;

        /// <summary>
        /// 慣性制約データ
        /// </summary>
        internal InertiaConstraint.ConstraintData inertiaConstraintData;

        /// <summary>
        /// 距離制約データ
        /// </summary>
        internal DistanceConstraint.ConstraintData distanceConstraintData;

        /// <summary>
        /// 曲げ制約データ
        /// </summary>
        internal TriangleBendingConstraint.ConstraintData bendingConstraintData;

        //=========================================================================================
        /// <summary>
        /// 連動アニメーター
        /// ・カリング
        /// ・更新モード
        /// </summary>
        internal Animator interlockingAnimator = null;

        /// <summary>
        /// カリング用アニメーター配下のレンダラーリスト
        /// </summary>
        internal List<Renderer> interlockingAnimatorRenderers = new List<Renderer>();

        //=========================================================================================
        /// <summary>
        /// キャンセルトークン
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();
        volatile object lockObject = new object();
        volatile object lockState = new object();

        /// <summary>
        /// 初期化待機カウンター
        /// </summary>
        volatile int suspendCounter = 0;

        /// <summary>
        /// 破棄フラグ
        /// </summary>
        volatile bool isDestory = false;

        /// <summary>
        /// 内部データまで完全に破棄されたかどうか
        /// </summary>
        volatile bool isDestoryInternal = false;

        /// <summary>
        /// 構築中フラグ
        /// </summary>
        volatile bool isBuild = false;

        public BitField32 GetStateFlag()
        {
            return default;
        }

        public bool IsState(int state)
        {
            return default;
        }

        public void SetState(int state, bool sw)
        {
        }

        public bool IsValid() => IsState(State_Valid);
        public bool IsCullingInvisible() => IsState(State_CullingInvisible);
        public bool IsCullingKeep() => IsState(State_CullingKeep);
        public bool IsSkipWriting() => IsState(State_SkipWriting);

        public bool IsEnable
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return MagicaManager.Team.IsEnable(TeamId);
            }
        }

        public bool HasProxyMesh
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return ProxyMeshContainer?.shareVirtualMesh?.IsSuccess ?? false;
            }
        }

        public string Name => cloth != null ? cloth.name : "(none)";

        //=========================================================================================
        public ClothProcess()
        {
        }

        public void Dispose()
        {
        }

        void DisposeInternal()
        {
        }

        internal void IncrementSuspendCounter()
        {
        }

        internal void DecrementSuspendCounter()
        {
        }

        internal int GetSuspendCounter()
        {
            return default;
        }

        public RenderMeshInfo GetRenderMeshInfo(int index)
        {
            return default;
        }

        internal void SyncParameters()
        {
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }

        internal void SetSkipWriting(bool sw)
        {
        }

        internal ClothUpdateMode GetClothUpdateMode()
        {
            return default;
        }
    }
}
