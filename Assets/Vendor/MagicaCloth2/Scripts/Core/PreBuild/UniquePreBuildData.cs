// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildデータの固有部分
    /// </summary>
    [System.Serializable]
    public class UniquePreBuildData : ITransform
    {
        public int version;
        public ResultCode buildResult;

        public List<RenderSetupData.UniqueSerializationData> renderSetupDataList = new List<RenderSetupData.UniqueSerializationData>();

        public VirtualMesh.UniqueSerializationData proxyMesh;
        public List<VirtualMesh.UniqueSerializationData> renderMeshList = new List<VirtualMesh.UniqueSerializationData>();

        //=========================================================================================
        public ResultCode DataValidate()
        {
            return default;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }
    }
}
