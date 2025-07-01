// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] The general type of object an <see cref="AnimationClip"/> can animate.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimationType
    /// 
    public enum AnimationType
    {
        /// <summary>Unable to determine a type.</summary>
        None,

        /// <summary>A Humanoid rig.</summary>
        Humanoid,

        /// <summary>A Generic rig.</summary>
        Generic,

        /// <summary>A <see cref="Generic"/> rig which only animates a <see cref="SpriteRenderer.sprite"/>.</summary>
        Sprite,
    }

    /// <summary>[Editor-Only]
    /// Various utility functions relating to the properties animated by an <see cref="AnimationClip"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimationBindings
    /// 
    public class AnimationBindings : AssetPostprocessor
    {
        /************************************************************************************************************************/
        #region Animation Types
        /************************************************************************************************************************/

        private static Dictionary<AnimationClip, bool> _ClipToIsSprite;

        /// <summary>Determines the <see cref="AnimationType"/> of the specified `clip`.</summary>
        public static AnimationType GetAnimationType(AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Determines the <see cref="AnimationType"/> of the specified `animator`.</summary>
        public static AnimationType GetAnimationType(Animator animator)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Determines the <see cref="AnimationType"/> of the specified `gameObject`.</summary>
        public static AnimationType GetAnimationType(GameObject gameObject)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        private static bool _CanGatherBindings = true;

        /// <summary>No more than one set of bindings should be gathered per frame.</summary>
        private static bool CanGatherBindings()
        {
            return default;
        }

        /************************************************************************************************************************/

        private static Dictionary<GameObject, BindingData> _ObjectToBindings;

        /// <summary>Returns a cached <see cref="BindingData"/> representing the specified `gameObject`.</summary>
        /// <remarks>Note that the cache is cleared by <see cref="EditorApplication.hierarchyChanged"/>.</remarks>
        public static BindingData GetBindings(GameObject gameObject, bool forceGather = true)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static Dictionary<AnimationClip, EditorCurveBinding[]> _ClipToBindings;

        /// <summary>Returns a cached array of all properties animated by the specified `clip`.</summary>
        public static EditorCurveBinding[] GetBindings(AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Called when Unity imports an animation.</summary>
        protected virtual void OnPostprocessAnimation(GameObject root, AnimationClip clip)
            => OnAnimationChanged(clip);

        /// <summary>Clears any cached values relating to the `clip` since they may no longer be correct.</summary>
        public static void OnAnimationChanged(AnimationClip clip)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Clears all cached values in this class.</summary>
        public static void ClearCache()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// A collection of data about the properties on a <see cref="UnityEngine.GameObject"/> and its children
        /// which can be animated and the relationships between those properties and the properties that individual
        /// <see cref="AnimationClip"/>s are trying to animate.
        /// </summary>
        public class BindingData
        {
            /************************************************************************************************************************/

            /// <summary>The target object that this data represents.</summary>
            public readonly GameObject GameObject;

            /// <summary>Creates a new <see cref="BindingData"/> representing the specified `gameObject`.</summary>
            public BindingData(GameObject gameObject) => GameObject = gameObject;

            /************************************************************************************************************************/

            private AnimationType? _ObjectType;

            /// <summary>The cached <see cref="AnimationType"/> of the <see cref="GameObject"/>.</summary>
            public AnimationType ObjectType
            {
                get
                {
                    if (_ObjectType == null)
                        _ObjectType = GetAnimationType(GameObject);
                    return _ObjectType.Value;
                }
            }

            /************************************************************************************************************************/

            private HashSet<EditorCurveBinding> _ObjectBindings;

            /// <summary>The cached properties of the <see cref="GameObject"/> and its children which can be animated.</summary>
            public HashSet<EditorCurveBinding> ObjectBindings
            {
                get
                {
                    if (_ObjectBindings == null)
                    {
                        _ObjectBindings = new HashSet<EditorCurveBinding>();
                        var transforms = GameObject.GetComponentsInChildren<Transform>();
                        for (int i = 0; i < transforms.Length; i++)
                        {
                            var bindings = AnimationUtility.GetAnimatableBindings(transforms[i].gameObject, GameObject);
                            _ObjectBindings.UnionWith(bindings);
                        }
                    }

                    return _ObjectBindings;
                }
            }

            /************************************************************************************************************************/

            private HashSet<string> _ObjectTransformBindings;

            /// <summary>
            /// The <see cref="EditorCurveBinding.path"/> of all <see cref="Transform"/> bindings in
            /// <see cref="ObjectBindings"/>.
            /// </summary>
            public HashSet<string> ObjectTransformBindings
            {
                get
                {
                    if (_ObjectTransformBindings == null)
                    {
                        _ObjectTransformBindings = new HashSet<string>();
                        foreach (var binding in ObjectBindings)
                        {
                            if (binding.type == typeof(Transform))
                                _ObjectTransformBindings.Add(binding.path);
                        }
                    }

                    return _ObjectTransformBindings;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Determines the <see cref="MatchType"/> representing the properties animated by the `state` in
            /// comparison to the properties that actually exist on the target <see cref="GameObject"/> and its
            /// children.
            /// <para></para>
            /// Also compiles a `message` explaining the differences if that paraneter is not null.
            /// </summary>
            public MatchType GetMatchType(Animator animator, AnimancerState state, StringBuilder message, bool forceGather = true)
            {
                return default;
            }

            /************************************************************************************************************************/

            private const string LinePrefix = "- ";

            private Dictionary<AnimationClip, MatchType> _BindingMatches;

            /// <summary>
            /// Determines the <see cref="MatchType"/> representing the properties animated by the `clip` in
            /// comparison to the properties that actually exist on the target <see cref="GameObject"/> and its
            /// children.
            /// <para></para>
            /// Also compiles a `message` explaining the differences if that paraneter is not null.
            /// </summary>
            public MatchType GetMatchType(AnimationClip clip, StringBuilder message,
                Dictionary<EditorCurveBinding, bool> bindingsInMessage, ref int existingBindings, bool forceGather = true)
            {
                return default;
            }

            /************************************************************************************************************************/

            private MatchType GetMatchType(EditorCurveBinding[] bindings,
                Dictionary<EditorCurveBinding, bool> bindingsInMessage, ref int existingBindings)
            {
                return default;
            }

            /************************************************************************************************************************/

            private static bool ShouldIgnoreBinding(EditorCurveBinding binding)
            {
                return default;
            }

            /************************************************************************************************************************/

            private bool MatchesObjectBinding(EditorCurveBinding binding)
            {
                return default;
            }

            /************************************************************************************************************************/

            private static void AppendBindings(StringBuilder message, Dictionary<EditorCurveBinding, bool> bindings, int existingBindings)
            {
            }

            /************************************************************************************************************************/

            private static class TransformBindings
            {
                [Flags]
                private enum Flags
                {
                    None = 0,

                    PositionX = 1 << 0,
                    PositionY = 1 << 1,
                    PositionZ = 1 << 2,

                    RotationX = 1 << 3,
                    RotationY = 1 << 4,
                    RotationZ = 1 << 5,
                    RotationW = 1 << 6,

                    EulerX = 1 << 7,
                    EulerY = 1 << 8,
                    EulerZ = 1 << 9,

                    ScaleX = 1 << 10,
                    ScaleY = 1 << 11,
                    ScaleZ = 1 << 12,
                }

                private static bool HasAll(Flags flag, Flags has) => (flag & has) == has;

                private static bool HasAny(Flags flag, Flags has) => (flag & has) != Flags.None;

                /************************************************************************************************************************/

                private static readonly Flags[]
                    PositionFlags = { Flags.PositionX, Flags.PositionY, Flags.PositionZ },
                    RotationFlags = { Flags.RotationX, Flags.RotationY, Flags.RotationZ, Flags.RotationW },
                    EulerFlags = { Flags.EulerX, Flags.EulerY, Flags.EulerZ },
                    ScaleFlags = { Flags.ScaleX, Flags.ScaleY, Flags.ScaleZ };

                /************************************************************************************************************************/

                public static bool Append(Dictionary<EditorCurveBinding, bool> bindings,
                    List<EditorCurveBinding> sortedBindings, ref int index, StringBuilder message)
                {
                    return default;
                }

                /************************************************************************************************************************/

                private static Flags GetFlags(Dictionary<EditorCurveBinding, bool> bindings,
                    List<EditorCurveBinding> sortedBindings, ref int index, List<EditorCurveBinding> otherBindings, out bool anyExists)
                {
                    anyExists = default(bool);
                    return default;
                }

                /************************************************************************************************************************/

                private static void AppendProperty(StringBuilder message, ref bool first, Flags flags,
                    Flags[] propertyFlags, string propertyName, string flagNames)
                {
                }

                /************************************************************************************************************************/

                private static StringBuilder AppendSeparator(StringBuilder message, ref bool first, string prefix, string separator)
                {
                    return default;
                }

                /************************************************************************************************************************/
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Logs a description of the issues found when comparing the properties animated by the `state` to the
            /// properties that actually exist on the target <see cref="GameObject"/> and its children.
            /// </summary>
            public void LogIssues(AnimancerState state, MatchType match)
            {
            }

            /************************************************************************************************************************/

            /// <summary>[Internal] Removes any cached values relating to the `clip`.</summary>
            internal void OnAnimationChanged(AnimationClip clip)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #region GUI
        /************************************************************************************************************************/

        /// <summary>
        /// A summary of the compatability between the properties animated by an <see cref="AnimationClip"/> and the
        /// properties that actually exist on a particular <see cref="GameObject"/> (and its children).
        /// </summary>
        public enum MatchType
        {
            /// <summary>All properties exist.</summary>
            Correct,

            /// <summary>Not yet checked.</summary>
            Unknown,

            /// <summary>The <see cref="AnimationClip"/> does not animate anything.</summary>
            Empty,

            /// <summary>Some of the animated properties do not exist on the object.</summary>
            Warning,

            /// <summary>None of the animated properties exist on the object.</summary>
            Error,
        }

        /************************************************************************************************************************/

        private static readonly GUIStyle ButtonStyle = new GUIStyle();// No margins or anything.

        /************************************************************************************************************************/

        private static readonly int ButtonHash = "Button".GetHashCode();

        /// <summary>
        /// Draws a <see cref="GUI.Button(Rect, GUIContent, GUIStyle)"/> indicating the <see cref="MatchType"/> of the
        /// `state` compared to the object it is being played on.
        /// <para></para>
        /// Clicking the button calls <see cref="BindingData.LogIssues"/>.
        /// </summary>
        public static void DoBindingMatchGUI(ref Rect area, AnimancerState state)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Icons
        /************************************************************************************************************************/

        /// <summary>Get an icon = corresponding to the specified <see cref="MatchType"/>.</summary>
        public static Texture GetIcon(MatchType match)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>A unit test to make sure that the icons are properly loaded.</summary>
        public static void AssertIcons()
        {
        }

        /************************************************************************************************************************/

        private static class Icons
        {
            /************************************************************************************************************************/

            public static readonly Texture Empty = AnimancerGUI.LoadIcon("console.infoicon.sml");
            public static readonly Texture Warning = AnimancerGUI.LoadIcon("console.warnicon.sml");
            public static readonly Texture Error = AnimancerGUI.LoadIcon("console.erroricon.sml");

            /************************************************************************************************************************/

            public static readonly Texture[] Unknown =
            {
                AnimancerGUI.LoadIcon("WaitSpin00"),
                AnimancerGUI.LoadIcon("WaitSpin01"),
                AnimancerGUI.LoadIcon("WaitSpin02"),
                AnimancerGUI.LoadIcon("WaitSpin03"),
                AnimancerGUI.LoadIcon("WaitSpin04"),
                AnimancerGUI.LoadIcon("WaitSpin05"),
                AnimancerGUI.LoadIcon("WaitSpin06"),
                AnimancerGUI.LoadIcon("WaitSpin07"),
                AnimancerGUI.LoadIcon("WaitSpin08"),
                AnimancerGUI.LoadIcon("WaitSpin09"),
                AnimancerGUI.LoadIcon("WaitSpin10"),
                AnimancerGUI.LoadIcon("WaitSpin11"),
            };

            public static Texture GetUnknown()
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

