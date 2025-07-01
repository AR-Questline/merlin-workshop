// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// トランスフォーム情報の一時記録
    /// </summary>
    public class TransformRecord : IValid, ITransform
    {
        public Transform transform;
        public int id;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale; // lossy scale
        public Matrix4x4 localToWorldMatrix;
        public Matrix4x4 worldToLocalMatrix;
        public int pid;

        public TransformRecord(Transform t)
        {
        }

        public Vector3 InverseTransformDirection(Vector3 dir)
        {
            return default;
        }

        public bool IsValid()
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
