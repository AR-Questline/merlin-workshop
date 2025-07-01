// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class RenderSetupData
    {
        /// <summary>
        /// PreBuildの共有部分保存データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public ResultCode result;
            public string name;
            public SetupType setupType;

            // Mesh ---------------------------------------------------------------
            public Mesh originalMesh;
            public int vertexCount;
            public bool hasSkinnedMesh;
            public bool hasBoneWeight;
            public int skinRootBoneIndex;
            public int skinBoneCount;
            // MeshDataでは取得できないメッシュ情報
            public List<Matrix4x4> bindPoseList;
            public byte[] bonesPerVertexArray;
            public byte[] boneWeightArray;
            public Vector3[] localPositions;
            public Vector3[] localNormals;

            // Bone ---------------------------------------------------------------
            public BoneConnectionMode boneConnectionMode;

            // Common -------------------------------------------------------------
            public int renderTransformIndex;
        }

        public ShareSerializationData ShareSerialize()
        {
            return default;
        }

        public static RenderSetupData ShareDeserialize(ShareSerializationData sdata)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuild固有部分の保存データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public ResultCode result;

            // Mesh ---------------------------------------------------------------
            public Renderer renderer;
            public SkinnedMeshRenderer skinRenderer;
            public MeshFilter meshFilter;
            public Mesh originalMesh;

            // Common -------------------------------------------------------------
            public List<Transform> transformList;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            return default;
        }
    }
}
