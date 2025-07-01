// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>A set of up/right/down/left animations.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/DirectionalAnimationSet
    /// 
    [CreateAssetMenu(menuName = Strings.MenuPrefix + "Directional Animation Set/4 Directions", order = Strings.AssetMenuOrder + 10)]
    [HelpURL(Strings.DocsURLs.APIDocumentation + "/" + nameof(DirectionalAnimationSet))]
    public class DirectionalAnimationSet : ScriptableObject, IAnimationClipSource
    {
        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _Up;

        /// <summary>[<see cref="SerializeField"/>] The animation facing up (0, 1).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip Up
        {
            get => _Up;
            set
            {
                AssertCanSetClips();
                _Up = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _Right;

        /// <summary>[<see cref="SerializeField"/>] The animation facing right (1, 0).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip Right
        {
            get => _Right;
            set
            {
                AssertCanSetClips();
                _Right = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _Down;

        /// <summary>[<see cref="SerializeField"/>] The animation facing down (0, -1).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip Down
        {
            get => _Down;
            set
            {
                AssertCanSetClips();
                _Down = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private AnimationClip _Left;

        /// <summary>[<see cref="SerializeField"/>] The animation facing left (-1, 0).</summary>
        /// <exception cref="ArgumentException"><see cref="AllowSetClips"/> was not called before setting this value.</exception>
        public AnimationClip Left
        {
            get => _Left;
            set
            {
                AssertCanSetClips();
                _Left = value;
                AnimancerUtilities.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        private bool _AllowSetClips;
#endif

        /// <summary>[Assert-Only] Determines whether the <see cref="AnimationClip"/> properties are allowed to be set.</summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public void AllowSetClips(bool allow = true)
        {
        }

        /// <summary>[Assert-Only] Throws an <see cref="ArgumentException"/> if <see cref="AllowSetClips"/> was not called.</summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public void AssertCanSetClips()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns the animation closest to the specified `direction`.</summary>
        public virtual AnimationClip GetClip(Vector2 direction)
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Directions
        /************************************************************************************************************************/

        /// <summary>The number of animations in this set.</summary>
        public virtual int ClipCount => 4;

        /************************************************************************************************************************/

        /// <summary>Up, Right, Down, or Left.</summary>
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/directional-sets">Directional Animation Sets</see>
        /// </remarks>
        /// https://kybernetik.com.au/animancer/api/Animancer/Direction
        /// 
        public enum Direction
        {
            /// <summary><see cref="Vector2.up"/>.</summary>
            Up,

            /// <summary><see cref="Vector2.right"/>.</summary>
            Right,

            /// <summary><see cref="Vector2.down"/>.</summary>
            Down,

            /// <summary><see cref="Vector2.left"/>.</summary>
            Left,
        }

        /************************************************************************************************************************/

        /// <summary>Returns the name of the specified `direction`.</summary>
        protected virtual string GetDirectionName(int direction) => ((Direction)direction).ToString();

        /************************************************************************************************************************/

        /// <summary>Returns the animation associated with the specified `direction`.</summary>
        public AnimationClip GetClip(Direction direction)
        {
            return default;
        }

        /// <summary>Returns the animation associated with the specified `direction`.</summary>
        public virtual AnimationClip GetClip(int direction) => GetClip((Direction)direction);

        /************************************************************************************************************************/

        /// <summary>Sets the animation associated with the specified `direction`.</summary>
        public void SetClip(Direction direction, AnimationClip clip)
        {
        }

        /// <summary>Sets the animation associated with the specified `direction`.</summary>
        public virtual void SetClip(int direction, AnimationClip clip) => SetClip((Direction)direction, clip);

        /************************************************************************************************************************/
        #region Conversion
        /************************************************************************************************************************/

        /// <summary>Returns a vector representing the specified `direction`.</summary>
        public static Vector2 DirectionToVector(Direction direction)
        {
            return default;
        }

        /// <summary>Returns a vector representing the specified `direction`.</summary>
        public virtual Vector2 GetDirection(int direction) => DirectionToVector((Direction)direction);

        /************************************************************************************************************************/

        /// <summary>Returns the direction closest to the specified `vector`.</summary>
        public static Direction VectorToDirection(Vector2 vector)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a copy of the `vector` pointing in the closest direction this set type has an animation for.</summary>
        public static Vector2 SnapVectorToDirection(Vector2 vector)
        {
            return default;
        }

        /// <summary>Returns a copy of the `vector` pointing in the closest direction this set has an animation for.</summary>
        public virtual Vector2 Snap(Vector2 vector) => SnapVectorToDirection(vector);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Collections
        /************************************************************************************************************************/

        /// <summary>Adds all animations from this set to the `clips`, starting from the specified `index`.</summary>
        public void AddClips(AnimationClip[] clips, int index)
        {
        }

        /// <summary>[<see cref="IAnimationClipSource"/>] Adds all animations from this set to the `clips`.</summary>
        public void GetAnimationClips(List<AnimationClip> clips)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds unit vectors corresponding to each of the animations in this set to the `directions`, starting from
        /// the specified `index`.
        /// </summary>
        public void AddDirections(Vector2[] directions, int index)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="AddClips"/> and <see cref="AddDirections"/>.</summary>
        public void AddClipsAndDirections(AnimationClip[] clips, Vector2[] directions, int index)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Editor Functions
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        [UnityEditor.CustomEditor(typeof(DirectionalAnimationSet), true), UnityEditor.CanEditMultipleObjects]
        private class Editor : Animancer.Editor.ScriptableObjectEditor { }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Attempts to assign the `clip` to one of this set's fields based on its name and returns the direction index
        /// of that field (or -1 if it was unable to determine the direction).
        /// </summary>
        public virtual int SetClipByName(AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/

        [UnityEditor.MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet) + "/Find Animations")]
        private static void FindSimilarAnimations(UnityEditor.MenuCommand command)
        {
        }

        /************************************************************************************************************************/

        [UnityEditor.MenuItem(Strings.CreateMenuPrefix + "Directional Animation Set/From Selection",
            priority = Strings.AssetMenuOrder + 12)]
        private static void CreateDirectionalAnimationSet()
        {
        }

        /************************************************************************************************************************/

        [UnityEditor.MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet) + "/Toggle Looping")]
        private static void ToggleLooping(UnityEditor.MenuCommand command)
        {
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}
