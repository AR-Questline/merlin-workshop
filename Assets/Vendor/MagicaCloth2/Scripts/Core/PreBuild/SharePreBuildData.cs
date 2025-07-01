// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildデータの共有部分
    /// </summary>
    [System.Serializable]
    public class SharePreBuildData
    {
        public int version;
        public string buildId;
        public ResultCode buildResult;
        public Vector3 buildScale;

        public List<RenderSetupData.ShareSerializationData> renderSetupDataList = new List<RenderSetupData.ShareSerializationData>();

        public VirtualMesh.ShareSerializationData proxyMesh;
        public List<VirtualMesh.ShareSerializationData> renderMeshList = new List<VirtualMesh.ShareSerializationData>();

        public DistanceConstraint.ConstraintData distanceConstraintData;
        public TriangleBendingConstraint.ConstraintData bendingConstraintData;
        public InertiaConstraint.ConstraintData inertiaConstraintData;

        //=========================================================================================
        public ResultCode DataValidate()
        {
            return default;
        }

        public bool CheckBuildId(string buildId)
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }
    }
}
