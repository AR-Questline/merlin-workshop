// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画対象の基本情報
    /// レンダラーまたはボーン構成情報
    /// この情報をもとに仮想メッシュなどを作成する
    /// またこの情報はキャラクターが動き出す前に取得しておく必要がある
    /// そのためAwake()などで実行する
    /// </summary>
    public partial class RenderSetupData : IDisposable, ITransform
    {
        public ResultCode result;
        public string name = string.Empty;
        public bool isManaged; // pre-build DeserializeManager管理

        // タイプ
        public enum SetupType
        {
            MeshCloth = 0,
            BoneCloth = 1,
            BoneSpring = 2,
        }
        public SetupType setupType;

        // Mesh ---------------------------------------------------------------
        // レンダラーとメッシュ情報
        public Renderer renderer;
        public SkinnedMeshRenderer skinRenderer;
        public MeshFilter meshFilter;
        public Mesh originalMesh;
        public int vertexCount;
        public bool hasSkinnedMesh; // SkinnedMeshRendererを利用しているかどうか
        public bool hasBoneWeight; // SkinnedMeshRendererでもボーンウエイトを持っていないケースあり！
        public Mesh.MeshDataArray meshDataArray;　// Jobで利用するためのMeshData
        public int skinRootBoneIndex;
        public int skinBoneCount;

        // MeshDataでは取得できないメッシュ情報
        public List<Matrix4x4> bindPoseList;
        public NativeArray<byte> bonesPerVertexArray;
        public NativeArray<BoneWeight1> boneWeightArray;

        // PreBuild時のみ保持する情報.逆にmeshDataArrayは持たない
        public NativeArray<Vector3> localPositions;
        public NativeArray<Vector3> localNormals;

        // Bone ---------------------------------------------------------------
        public List<int> rootTransformIdList;
        public enum BoneConnectionMode
        {
            // line only.
            // ラインのみ
            Line = 0,

            //Automatically mesh connection according to the interval of Transform.
            // Transformの間隔に従い自動でメッシュ接続
            AutomaticMesh = 1,

            //Generate meshes in the order of Transforms registered in RootList and connect the beginning and end in a loop.
            // RootListに登録されたTransformの順にメッシュを生成し、最初と最後をループ状に繋げる
            SequentialLoopMesh = 2,

            // Generate meshes in the order of Transforms registered in RootList, but do not connect the beginning and end.
            // RootListに登録されたTransformの順にメッシュを生成するが最初と最後を繋げない
            SequentialNonLoopMesh = 3,
        }
        public BoneConnectionMode boneConnectionMode = BoneConnectionMode.Line;
        public List<int> collisionBoneIndexList; // BoneSpringのコリジョン有効Transformインデックスリスト

        // Common -------------------------------------------------------------
        // Transform情報
        // 通常メッシュはrenderTransorm100%のスキニングとして扱われる
        public List<Transform> transformList; // skin bonesは[0]～skinBoneCountまで
        public int[] transformIdList;
        public int[] transformParentIdList; // 親ID(0=なし)
        public FixedList512Bytes<int>[] transformChildIdList; // 子IDリスト
        public NativeArray<float3> transformPositions;
        public NativeArray<quaternion> transformRotations;
        public NativeArray<float3> transformLocalPositins;
        public NativeArray<quaternion> transformLocalRotations;
        public NativeArray<float3> transformScales;
        public NativeArray<quaternion> transformInverseRotations;
        public int renderTransformIndex; // 描画基準トランスフォーム
        public float4x4 initRenderLocalToWorld; // 初期化時の基準マトリックス(LtoW)
        public float4x4 initRenderWorldtoLocal; // 初期化時の基準マトリックス(WtoL)
        public quaternion initRenderRotation; // 初期化時の基準回転
        public float3 initRenderScale; // 初期化時の基準スケール

        public bool IsSuccess() => result.IsSuccess();
        public bool IsFaild() => result.IsFaild();
        public int TransformCount => transformList?.Count ?? 0;
        public bool HasMeshDataArray => meshDataArray.Length > 0;
        public bool HasLocalPositions => localPositions.IsCreated;
        
        static readonly Type CharacterHandBaseType = Type.GetType("Awaken.TG.Main.Heroes.Combat.CharacterHandBase, TG.Main");

        //static readonly ProfilerMarker initProfiler = new ProfilerMarker("Render Setup");

        //=========================================================================================
        public RenderSetupData() {
        }

        /// <summary>
        /// レンダラーから基本情報を作成する（メインスレッドのみ）
        /// タイプはMeshになる
        /// </summary>
        /// <param name="ren"></param>
        public RenderSetupData(Renderer ren)
        {
        }

        [BurstCompile]
        struct VertexWeight5BoneCheckJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeReference<Define.Result> result;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// ルートボーンリストから基本情報を作成する（メインスレッドのみ）
        /// タイプはBoneになる
        /// </summary>
        /// <param name="renderTransform"></param>
        /// <param name="rootTransforms"></param>
        public RenderSetupData(
            SetupType setType,
            Transform renderTransform,
            List<Transform> rootTransforms,
            List<Transform> collisionBones,
            BoneConnectionMode connectionMode = BoneConnectionMode.Line,
            string name = "(no name)"
            )
        {
        }

        /// <summary>
        /// トランスフォーム情報の読み取り（メインスレッドのみ）
        /// この情報だけはキャラクターが動く前に取得する必要がある
        /// </summary>
        void ReadTransformInformation(bool includeChilds)
        {
        }

        /// <summary>
        /// 最低限のTransform情報を収集する
        /// </summary>
        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scales;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotations;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> inverseRotations;

            public void Execute(int index, TransformAccess transform)
            {
            }
        }

        public void Dispose()
        {
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 描画基準トランスフォームを取得する
        /// </summary>
        /// <returns></returns>
        public Transform GetRendeerTransform()
        {
            return default;
        }

        public int GetRenderTransformId()
        {
            return default;
        }

        public float4x4 GetRendeerLocalToWorldMatrix()
        {
            return default;
        }

        /// <summary>
        /// スキンレンダラーのルートトランスフォームを取得する
        /// </summary>
        /// <returns></returns>
        public Transform GetSkinRootTransform()
        {
            return default;
        }

        public int GetSkinRootTransformId()
        {
            return default;
        }

        public int GetTransformIndexFromId(int id)
        {
            return default;
        }

        /// <summary>
        /// 指定indexの親トランスフォームのインデックスを返す(-1=なし)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="centerExcluded">true=センタートランスフォームは除外する</param>
        /// <returns>なし(-1)</returns>
        public int GetParentTransformIndex(int index, bool centerExcluded)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// オリジナルメッシュのボーンウエイトをBoneWeight構造体のNativeArrayで取得する
        /// </summary>
        /// <param name="weights"></param>
        public void GetBoneWeightsRun(NativeArray<BoneWeight> weights)
        {
        }

        [BurstCompile]
        struct GetBoneWeightJos : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> boneWeightArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<BoneWeight> boneWeights;

            public void Execute()
            {
            }
        }
    }
}
