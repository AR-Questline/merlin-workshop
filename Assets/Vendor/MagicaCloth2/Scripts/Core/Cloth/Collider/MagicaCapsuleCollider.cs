// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Capsuleコライダーコンポーネント
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaCapsuleCollider")]
    [HelpURL("https://magicasoft.jp/en/mc2_capsulecollidercomponent/")]
    public class MagicaCapsuleCollider : ColliderComponent
    {
        public enum Direction
        {
            [InspectorName("X-Axis")]
            X = 0,

            [InspectorName("Y-Axis")]
            Y = 1,

            [InspectorName("Z-Axis")]
            Z = 2,
        }

        /// <summary>
        /// Reference transform axis.
        /// </summary>
        public Direction direction = Direction.X;

        /// <summary>
        /// 半径をStart/End別々に設定
        /// Set radius separately for Start/End.
        /// </summary>
        public bool radiusSeparation = false;

        /// <summary>
        /// 中央揃え
        /// Aligned on center.
        /// </summary>
        public bool alignedOnCenter = true;


        public override ColliderManager.ColliderType GetColliderType()
        {
            return default;
        }

        /// <summary>
        /// set size.
        /// </summary>
        /// <param name="startRadius"></param>
        /// <param name="endRadius"></param>
        /// <param name="length"></param>
        public void SetSize(float startRadius, float endRadius, float length)
        {
        }

        /// <summary>
        /// get size.
        /// (x:start radius, y:end radius, z:length)
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetSize()
        {
            return default;
        }

        /// <summary>
        /// カプセルのローカル方向を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalDir()
        {
            return default;
        }

        /// <summary>
        /// カプセルのローカル上方向を返す
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalUp()
        {
            return default;
        }

        public override void DataValidate()
        {
        }
    }
}
