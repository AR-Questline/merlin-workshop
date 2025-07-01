// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CapsuleColliderのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaCapsuleCollider))]
    public class MagicaCapsuleColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
}
