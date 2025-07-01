// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    public static class GizmoUtility
    {
        // ギズモカラー定義
        public static readonly Color ColorCollider = new Color(0.0f, 1.0f, 0.0f);
        public static readonly Color ColorNonSelectedCollider = new Color(0.5f, 0.3f, 0.0f);
        public static readonly Color ColorSkinningBone = new Color(1.0f, 0.5f, 0.0f);
        public static readonly Color ColorWindZone = new Color(1f, 1f, 1f);
        public static readonly Color ColorWindArrow = new Color(1f, 1f, 0f);

        public static readonly Quaternion FlipZ = Quaternion.AngleAxis(180.0f, Vector3.up);

        //=========================================================================================
        public static void SetColor(Color col, bool useHandles)
        {
        }

        public static void DrawSphere(Vector3 pos, float radius, bool useHandles)
        {
        }

        public static void DrawWireSphere(Vector3 pos, Quaternion rot, float radius, Quaternion camRot, bool useHandles)
        {
        }

        public static void DrawSimpleWireSphere(Vector3 pos, float radius, Quaternion camRot, bool useHandles)
        {
        }

        public static void DrawLine(Vector3 from, Vector3 to, bool useHandles)
        {
        }

        public static void DrawWireCapsule(Vector3 pos, Quaternion rot, Vector3 dir, Vector3 up, float sradius, float eradius, float len, bool alignedCenter, Quaternion camRot, bool useHandles)
        {
        }

        //public static void DrawWireCapsule(Vector3 spos, Vector3 epos, Quaternion rot, float sradius, float eradius, Quaternion camRot, bool useHandles)
        //{
        //    if (useHandles)
        //    {
        //        DrawWireSphere(spos, rot, sradius, camRot, true);
        //        DrawWireSphere(epos, rot, eradius, camRot, true);

        //        var ps = spos + camRot * Vector3.up * sradius;
        //        var es = epos + camRot * Vector3.up * eradius;
        //        Handles.DrawLine(ps, es);
        //    }
        //    else
        //    {

        //    }
        //}

        /// <summary>
        /// ワイヤーボックスを描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="resetMatrix"></param>
        public static void DrawWireCube(Vector3 pos, Quaternion rot, Vector3 size, bool useHandles)
        {
        }

        public static void DrawCross(Vector3 pos, Quaternion rot, float size, bool useHandles)
        {
        }

        //=========================================================================================
        public static void DrawCollider(ColliderComponent collider, Quaternion camRot, bool useHandles, bool selected)
        {
        }

        /// <summary>
        /// ワイヤーカプセルを描画する
        /// UnityのCapsuleColliderと同じ
        /// </summary>
        /// <param name="pos">基準座標</param>
        /// <param name="rot">基準回転</param>
        /// <param name="ldir">カプセルの方向</param>
        /// <param name="lup">カプセルの上方向</param>
        /// <param name="length">カプセルの長さ</param>
        /// <param name="startRadius">始点の半径</param>
        /// <param name="endRadius">終点の半径</param>
        public static void DrawWireCapsule(
            Vector3 pos, Quaternion rot, Vector3 scl,
            Vector3 ldir, Vector3 lup,
            float length, float startRadius, float endRadius,
            bool resetMatrix = true
            )
        {
        }

        /// <summary>
        /// ワイヤー球を描画する
        /// UnityのSphereColliderと同じ
        /// </summary>
        /// <param name="pos">基準座標</param>
        /// <param name="rot">基準回転</param>
        /// <param name="radius">半径</param>
        /// <param name="resetMatrix"></param>
        public static void DrawWireSphere(
            Vector3 pos, Quaternion rot, Vector3 scl, float radius,
            bool drawSphere, bool drawAxis,
            bool resetMatrix = true)
        {
        }

#if false
        /// <summary>
        /// ワイヤーボックスを描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="resetMatrix"></param>
        public static void DrawWireCube(Vector3 pos, Quaternion rot, Vector3 size, bool resetMatrix = true)
        {
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            if (resetMatrix)
                Gizmos.matrix = Matrix4x4.identity;
        }
#endif

        public static void DrawWireCone(Vector3 pos, Quaternion rot, float length, float radius, int div = 8)
        {
        }

        /// <summary>
        /// ワイヤー矢印を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="cross">十字描画</param>
        public static void DrawWireArrow(Vector3 pos, Quaternion rot, Vector3 size, bool cross = false)
        {
        }

        /// <summary>
        /// XYZ軸を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="resetMatrix"></param>
        public static void DrawAxis(Vector3 pos, Quaternion rot, float size, bool resetMatrix = true)
        {
        }

        /// <summary>
        /// ボーン形状を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="tpos"></param>
        /// <param name="size"></param>
        public static void DrawBone(Vector3 pos, Vector3 tpos, float size)
        {
        }

        //=========================================================================================
        /// <summary>
        /// Handlesによるコーンの描画
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="size"></param>
        /// <param name="angle">角度(deg)</param>
        /// <param name="coneColor"></param>
        /// <param name="wireColor"></param>
        /// <param name="wireThickness"></param>
        public static void ConeHandle(
            Vector3 pos, Quaternion rot, float size, float angle,
            Color coneColor, Color wireColor, float wireThickness = 1.0f,
            int controllId = 0
            )
        {
        }

        /// <summary>
        /// 扇を描画する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="angle"></param>
        /// <param name="size"></param>
        /// <param name="wireThickness"></param>
        /// <param name="arcColor1"></param>
        /// <param name="arcColor2"></param>
        /// <param name="wireColor"></param>
        public static void ArcHandle(
            Vector3 pos, Quaternion rot, float angle, float size, float wireThickness,
            Color arcColor1, Color arcColor2, Color wireColor
            )
        {
        }

        //=========================================================================================
        public static void DrawWindZone(MagicaWindZone windZone, Quaternion camRot, bool selected)
        {
        }
    }
}
