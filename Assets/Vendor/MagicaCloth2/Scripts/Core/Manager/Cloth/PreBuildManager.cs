// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildの管理マネージャ
    /// </summary>
    public class PreBuildManager : IManager, IValid
    {
        /// <summary>
        /// 共有ビルドデータの復元データ
        /// </summary>
        internal class ShareDeserializationData : IDisposable
        {
            internal string buildId;
            internal ResultCode result;
            internal int referenceCount;

            internal List<RenderSetupData> renderSetupDataList = new List<RenderSetupData>();
            internal VirtualMesh proxyMesh = null;
            internal List<VirtualMesh> renderMeshList = new List<VirtualMesh>();

            internal DistanceConstraint.ConstraintData distanceConstraintData;
            internal TriangleBendingConstraint.ConstraintData bendingConstraintData;
            internal InertiaConstraint.ConstraintData inertiaConstraintData;

            public void Dispose()
            {
            }

            public void Deserialize(SharePreBuildData sharePreBuilddata)
            {
            }

            public int RenderMeshCount => renderMeshList?.Count ?? 0;

            public VirtualMeshContainer GetProxyMeshContainer()
            {
                return default;
            }

            public VirtualMeshContainer GetRenderMeshContainer(int index)
            {
                return default;
            }
        }

        Dictionary<SharePreBuildData, ShareDeserializationData> deserializationDict = new Dictionary<SharePreBuildData, ShareDeserializationData>();
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

        public void InformationLog(StringBuilder allsb)
        {
        }

        //=========================================================================================
        static readonly ProfilerMarker deserializationProfiler = new ProfilerMarker("PreBuild.Deserialization");

        /// <summary>
        /// PreBuildDataをデシリアライズし登録する
        /// すでに登録されていた場合は参照カウンタを加算する
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="referenceIncrement"></param>
        /// <returns></returns>
        internal ShareDeserializationData RegisterPreBuildData(SharePreBuildData sdata, bool referenceIncrement)
        {
            return default;
        }

        internal ShareDeserializationData GetPreBuildData(SharePreBuildData sdata)
        {
            return default;
        }

        /// <summary>
        /// PreBuildDataのデシリアライズデータを解除する
        /// 参照カウンタが０でも破棄はしない
        /// </summary>
        /// <param name="sdata"></param>
        internal void UnregisterPreBuildData(SharePreBuildData sdata)
        {
        }

        /// <summary>
        /// 未使用のデシリアライズデータをすべて破棄する
        /// </summary>
        internal void UnloadUnusedData()
        {
        }
    }
}
