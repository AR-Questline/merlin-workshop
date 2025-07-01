// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// プロパティをアニメーションから制御するためのラッパー.
    /// Wrapper for controlling properties from animation.
    /// </summary>
    public partial class MagicaCloth
    {
        [HideInInspector]
        public float animationPoseRatioProperty;
        float _animationPoseRatioProperty;

        [HideInInspector]
        public float gravityProperty;
        float _gravityProperty;

        [HideInInspector]
        public float dampingProperty;
        float _dampingProperty;

        [HideInInspector]
        public float worldInertiaProperty;
        float _worldInertiaProperty;

        [HideInInspector]
        public float localInertiaProperty;
        float _localInertiaProperty;

        [HideInInspector]
        public float windInfluenceProperty;
        float _windInfluenceProperty;

        //=========================================================================================
        internal void InitAnimationProperty()
        {
        }

        /// <summary>
        /// アニメーションによりMagicaClothのプロパティが変更されたときに呼び出される.
        /// Called when a property of MagicaCloth changes due to animation.
        /// </summary>
        void OnDidApplyAnimationProperties()
        {
        }
    }
}

