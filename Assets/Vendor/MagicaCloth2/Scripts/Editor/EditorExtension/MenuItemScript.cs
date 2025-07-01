// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    public class MenuItemScript
    {
        //=========================================================================================
        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Cloth", priority = 200)]
        static void AddMagicaCloth()
        {
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Sphere Collider", priority = 200)]
        static void AddSphereCollider()
        {
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Capsule Collider", priority = 200)]
        static void AddCapsuleCollider()
        {
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Plane Collider", priority = 200)]
        static void AddPlaneCollider()
        {
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Wind Zone", priority = 200)]
        static void AddWindZone()
        {
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Settings", priority = 200)]
        static void AddSettings()
        {
        }

        /// <summary>
        /// ヒエラルキーにオブジェクトを１つ追加する
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        static GameObject AddObject(string objName, bool addParentName, bool autoScale = false)
        {
            return default;
        }

        //=========================================================================================
        [MenuItem("Tools/Magica Cloth2/Manager information", false)]
        static void DispClothManagerInfo()
        {
        }
    }
}
