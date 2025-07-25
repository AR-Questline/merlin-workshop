﻿// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
#if MC2_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// クロスのデバッグ表示設定
    /// </summary>
    [System.Serializable]
    public class ClothDebugSettings
    {
        public enum DebugAxis
        {
            None,
            Normal,
            All,
        }

        //=====================================================================
        // ■公開するもの
        //=====================================================================
        public bool enable = false;
        public bool ztest = false;
        public bool position = true;
        public DebugAxis axis = DebugAxis.None;
        public bool shape = false;
        public bool baseLine = false;
        public bool depth = false;
        public bool collider = true;
        public bool animatedPosition = false;
        public DebugAxis animatedAxis = DebugAxis.None;
        public bool animatedShape = false;
        public bool inertiaCenter = true;
        //public bool basicPosition = false;
        //public DebugAxis basicAxis = DebugAxis.None;
        //public bool basicShape = false;

        //=====================================================================
        // ■デバッグ用
        //=====================================================================
#if MC2_DEBUG
        //[Space]
        //[Header("[MC2_DEBUG]")]
        [Header("<<< MC2_DEBUG >>>")]
        [Range(0.003f, 0.1f)]
        public float pointSize = 0.01f;
        public bool referOldPos = false;
        public bool radius = true;
        public bool localNumber = false;
        public bool particleNumber = false;
        public bool triangleNumber = false;
        public bool friction = false;
        public bool staticFriction = false;
        public bool attribute = false;
        //public bool verticalDistanceConstraint = false;
        //public bool horizontalDistanceConstraint = false;
        public bool collisionNormal = false;
        public bool cellCube = false;
        public bool baseLinePos = false;
        public int vertexMinIndex = 0;
        public int vertexMaxIndex = 100000;
        public int triangleMinIndex = 0;
        public int triangleMaxIndex = 100000;
#endif

        //=========================================================================================
        public bool CheckParticleDrawing(int index)
        {
            return default;
        }

        public bool CheckTriangleDrawing(int index)
        {
            return default;
        }

        public bool CheckRadiusDrawing()
        {
            return default;
        }

        public float GetPointSize()
        {
            return default;
        }

        public float GetLineSize()
        {
            return default;
        }

        public float GetInertiaCenterRadius()
        {
            return default;
        }

        public bool IsReferOldPos()
        {
            return default;
        }
    }
}
