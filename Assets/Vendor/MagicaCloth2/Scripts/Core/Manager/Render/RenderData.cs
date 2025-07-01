// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画対象の管理情報
    /// レンダラーまたはボーンの描画反映を行う
    /// </summary>
    public class RenderData : IDisposable, ITransform
    {
        /// <summary>
        /// 参照カウント。０になると破棄される
        /// </summary>
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// 利用中のプロセス（＝利用カウント）
        /// </summary>
        HashSet<ClothProcess> useProcessSet = new HashSet<ClothProcess>();

        /// <summary>
        /// Meshへの書き込み停止フラグ
        /// </summary>
        bool isSkipWriting;

        //=========================================================================================
        // セットアップデータ
        internal RenderSetupData setupData;
        internal RenderSetupData.UniqueSerializationData preBuildUniqueSerializeData;

        internal string Name => setupData?.name ?? "(empty)";

        internal bool HasSkinnedMesh => setupData?.hasSkinnedMesh ?? false;
        internal bool HasBoneWeight => setupData?.hasBoneWeight ?? false;

        //=========================================================================================
        // オリジナル情報
        Mesh originalMesh;
        SkinnedMeshRenderer skinnedMeshRendere;
        MeshFilter meshFilter;
        List<Transform> transformList;

        // カスタムメッシュ情報
        Mesh customMesh;
        NativeArray<Vector3> localPositions;
        NativeArray<Vector3> localNormals;
        NativeArray<BoneWeight> boneWeights;
        BoneWeight centerBoneWeight;

        /// <summary>
        /// カスタムメッシュの状態フラグ
        /// </summary>
        private const int Flag_UseCustomMesh = 0; // カスタムメッシュの利用
        private const int Flag_ChangePositionNormal = 1; // 座標および法線の書き込み
        private const int Flag_ChangeBoneWeight = 2; // ボーンウエイトの書き込み
        private const int Flag_ModifyBoneWeight = 3; // ボーンウエイトの変更

        private BitField32 flag;

        public bool UseCustomMesh => flag.IsSet(Flag_UseCustomMesh);

        //=========================================================================================
        public void Dispose()
        {
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }

        /// <summary>
        /// 初期化（メインスレッドのみ）
        /// この処理はスレッド化できないので少し負荷がかかるが即時実行する
        /// </summary>
        /// <param name="ren"></param>
        internal void Initialize(Renderer ren, RenderSetupData referenceSetupData, RenderSetupData.UniqueSerializationData referencePreBuildUniqueSetupData)
        {
        }

        internal ResultCode Result => setupData?.result ?? ResultCode.None;

        //=========================================================================================
        internal int AddReferenceCount()
        {
            return default;
        }

        internal int RemoveReferenceCount()
        {
            return default;
        }

        //=========================================================================================
        void SwapCustomMesh()
        {
        }

        void ResetCustomMeshWorkData()
        {
        }

        /// <summary>
        /// オリジナルメッシュに戻す
        /// </summary>
        void SwapOriginalMesh()
        {
        }

        /// <summary>
        /// レンダラーにメッシュを設定する
        /// </summary>
        /// <param name="mesh"></param>
        void SetMesh(Mesh mesh)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 利用の開始
        /// 利用するということはメッシュに頂点を書き込むことを意味する
        /// 通常コンポーネントがEnableになったときに行う
        /// </summary>
        public void StartUse(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// 利用の停止
        /// 停止するということはメッシュに頂点を書き込まないことを意味する
        /// 通常コンポーネントがDisableになったときに行う
        /// </summary>
        public void EndUse(ClothProcess cprocess)
        {
        }

        internal void UpdateUse(ClothProcess cprocess, int add)
        {
        }

        //=========================================================================================
        /// <summary>
        /// Meshへの書き込みフラグを更新する
        /// </summary>
        internal void UpdateSkipWriting()
        {
        }

        //=========================================================================================
        internal void WriteMesh()
        {
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの位置法線を更新
        /// </summary>
        /// <param name="mappingChunk"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle UpdatePositionNormal(DataChunk mappingChunk, JobHandle jobHandle = default)
        {
            return default;
        }

        [BurstCompile]
        struct UpdatePositionNormalJob2 : IJobParallelFor
        {
            public int startIndex;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> meshLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> meshLocalNormals;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> mappingReferenceIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingNormals;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// メッシュのボーンウエイト書き込み
        /// </summary>
        /// <param name="vmesh"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle UpdateBoneWeight(DataChunk mappingChunk, JobHandle jobHandle = default)
        {
            return default;
        }

        [BurstCompile]
        struct UpdateBoneWeightJob2 : IJobParallelFor
        {
            public int startIndex;
            public BoneWeight centerBoneWeight;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<BoneWeight> meshBoneWeights;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> mappingReferenceIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        public override string ToString()
        {
            return default;
        }
    }
}
