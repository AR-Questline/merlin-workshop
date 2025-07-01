// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using UnityEngine;

namespace Animancer
{
    /// <summary>A set of up/right/down/left animations with diagonals as well.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/DirectionalAnimationSet8
    /// 
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Directional Animation Set/8 Directions", order = Strings.AssetMenuOrder + 11)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(DirectionalAnimationSet8))]
    public class DirectionalAnimationSet8 : DirectionalAnimationSet
    {
        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _UpRight;

        /// <summary>[<see cref="SerializeField"/>] The animation facing diagonally up-right ~(0.7, 0.7).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip UpRight
        {
            get => _UpRight;
            set
            {
                AssertCanSetClips();
                _UpRight = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _DownRight;

        /// <summary>[<see cref="SerializeField"/>] The animation facing diagonally down-right ~(0.7, -0.7).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip DownRight
        {
            get => _DownRight;
            set
            {
                AssertCanSetClips();
                _DownRight = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _DownLeft;

        /// <summary>[<see cref="SerializeField"/>] The animation facing diagonally down-left ~(-0.7, -0.7).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip DownLeft
        {
            get => _DownLeft;
            set
            {
                AssertCanSetClips();
                _DownLeft = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _UpLeft;

        /// <summary>[<see cref="SerializeField"/>] The animation facing diagonally up-left ~(-0.7, 0.7).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip UpLeft
        {
            get => _UpLeft;
            set
            {
                AssertCanSetClips();
                _UpLeft = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns the animation closest to the specified `direction`.</summary>
        public override AnimationClip GetClip(Vector2 direction)
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Directions
        /************************************************************************************************************************/

        /// <summary>Constants for each of the diagonal directions.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
        /// </remarks>
        /// https://kybernetik.com.au/animancer/api/Animancer/Diagonals
        /// 
        public static class Diagonals
        {
            /************************************************************************************************************************/

            /// <summary>1 / (Square Root of 2).</summary>
            public const float OneOverSqrt2 = 0.70710678118f;

            /// <summary>A vector with a magnitude of 1 pointing up to the right.</summary>
            /// <remarks>The value is approximately (0.7, 0.7).</remarks>
            public static Vector2 UpRight => new Vector2(OneOverSqrt2, OneOverSqrt2);

            /// <summary>A vector with a magnitude of 1 pointing down to the right.</summary>
            /// <remarks>The value is approximately (0.7, -0.7).</remarks>
            public static Vector2 DownRight => new Vector2(OneOverSqrt2, -OneOverSqrt2);

            /// <summary>A vector with a magnitude of 1 pointing down to the left.</summary>
            /// <remarks>The value is approximately (-0.7, -0.7).</remarks>
            public static Vector2 DownLeft => new Vector2(-OneOverSqrt2, -OneOverSqrt2);

            /// <summary>A vector with a magnitude of 1 pointing up to the left.</summary>
            /// <remarks>The value is approximately (-0.707, 0.707).</remarks>
            public static Vector2 UpLeft => new Vector2(-OneOverSqrt2, OneOverSqrt2);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public override int ClipCount => 8;

        /************************************************************************************************************************/

        /// <summary>Up, Right, Down, Left, or their diagonals.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
        /// </remarks>
        /// https://kybernetik.com.au/animancer/api/Animancer/Direction
        /// 
        public new enum Direction
        {
            /// <summary><see cref="Vector2.up"/>.</summary>
            Up,

            /// <summary><see cref="Vector2.right"/>.</summary>
            Right,

            /// <summary><see cref="Vector2.down"/>.</summary>
            Down,

            /// <summary><see cref="Vector2.left"/>.</summary>
            Left,

            /// <summary><see cref="Vector2"/>(0.7..., 0.7...).</summary>
            UpRight,

            /// <summary><see cref="Vector2"/>(0.7..., -0.7...).</summary>
            DownRight,

            /// <summary><see cref="Vector2"/>(-0.7..., -0.7...).</summary>
            DownLeft,

            /// <summary><see cref="Vector2"/>(-0.7..., 0.7...).</summary>
            UpLeft,
        }

        /************************************************************************************************************************/

        protected override string GetDirectionName(int direction) => ((Direction)direction).ToString();

        /************************************************************************************************************************/

        /// <summary>Returns the animation associated with the specified `direction`.</summary>
        public AnimationClip GetClip(Direction direction)
        {
            return default;
        }

        public override AnimationClip GetClip(int direction) => GetClip((Direction)direction);

        /************************************************************************************************************************/

        /// <summary>Sets the animation associated with the specified `direction`.</summary>
        public void SetClip(Direction direction, AnimationClip clip)
        {
        }

        public override void SetClip(int direction, AnimationClip clip) => SetClip((Direction)direction, clip);

        /************************************************************************************************************************/

        /// <summary>Returns a vector representing the specified `direction`.</summary>
        public static Vector2 DirectionToVector(Direction direction)
        {
            return default;
        }

        public override Vector2 GetDirection(int direction) => DirectionToVector((Direction)direction);

        /************************************************************************************************************************/

        /// <summary>Returns the direction closest to the specified `vector`.</summary>
        public new static Direction VectorToDirection(Vector2 vector)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a copy of the `vector` pointing in the closest direction this set type has an animation for.</summary>
        public new static Vector2 SnapVectorToDirection(Vector2 vector)
        {
            return default;
        }

        public override Vector2 Snap(Vector2 vector) => SnapVectorToDirection(vector);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Name Based Operations
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        public override int SetClipByName(AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}
