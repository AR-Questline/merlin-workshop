// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshの共有部分と固有部分を１つにまとめた情報
    /// </summary>
    public class VirtualMeshContainer : IDisposable
    {
        public VirtualMesh shareVirtualMesh;
        public VirtualMesh.UniqueSerializationData uniqueData;

        public VirtualMeshContainer()
        {
        }

        public VirtualMeshContainer(VirtualMesh vmesh)
        {
        }

        public void Dispose()
        {
        }

        //=========================================================================================
        public bool hasUniqueData => uniqueData != null;

        //=========================================================================================
        public int GetTransformCount()
        {
            return default;
        }

        public Transform GetTransformFromIndex(int index)
        {
            return default;
        }

        public Transform GetCenterTransform()
        {
            return default;
        }
    }
}
