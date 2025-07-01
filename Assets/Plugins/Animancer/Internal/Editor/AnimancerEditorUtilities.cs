// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Various utilities used throughout Animancer.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerEditorUtilities
    /// 
    public static partial class AnimancerEditorUtilities
    {
        /************************************************************************************************************************/
        #region Misc
        /************************************************************************************************************************/

        /// <summary>Commonly used <see cref="BindingFlags"/> combinations.</summary>
        public const BindingFlags
            AnyAccessBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            StaticBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Editor-Only]
        /// Returns the first <typeparamref name="TAttribute"/> attribute on the `member` or <c>null</c> if there is none.
        /// </summary>
        public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider member, bool inherit = false)
            where TAttribute : class
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Animancer Extension] [Editor-Only] Is the <see cref="Vector2.x"/> or <see cref="Vector2.y"/> NaN?</summary>
        public static bool IsNaN(this Vector2 vector) => float.IsNaN(vector.x) || float.IsNaN(vector.y);

        /// <summary>[Animancer Extension] [Editor-Only] Is the <see cref="Vector3.x"/>, <see cref="Vector3.y"/>, or <see cref="Vector3.z"/> NaN?</summary>
        public static bool IsNaN(this Vector3 vector) => float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);

        /************************************************************************************************************************/

        /// <summary>Finds an asset of the specified type anywhere in the project.</summary>
        public static T FindAssetOfType<T>() where T : Object
        {
            return default;
        }

        /************************************************************************************************************************/

        // The "g" format gives a lower case 'e' for exponentials instead of upper case 'E'.
        private static readonly ConversionCache<float, string>
            FloatToString = new ConversionCache<float, string>((value) => $"{value:g}");

        /// <summary>[Animancer Extension]
        /// Calls <see cref="float.ToString(string)"/> using <c>"g"</c> as the format and caches the result.
        /// </summary>
        public static string ToStringCached(this float value) => FloatToString.Convert(value);

        /************************************************************************************************************************/

        /// <summary>The most recent <see cref="PlayModeStateChange"/>.</summary>
        public static PlayModeStateChange PlayModeState { get; private set; }

        /// <summary>Is the Unity Editor is currently changing between Play Mode and Edit Mode?</summary>
        public static bool IsChangingPlayMode =>
            PlayModeState == PlayModeStateChange.ExitingEditMode ||
            PlayModeState == PlayModeStateChange.ExitingPlayMode;

        [InitializeOnLoadMethod]
        private static void WatchForPlayModeChanges()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Collections
        /************************************************************************************************************************/

        /// <summary>Adds default items or removes items to make the <see cref="List{T}.Count"/> equal to the `count`.</summary>
        public static void SetCount<T>(List<T> list, int count)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes any items from the `list` that are <c>null</c> and items that appear multiple times.
        /// Returns true if the `list` was modified.
        /// </summary>
        public static bool RemoveMissingAndDuplicates(ref List<GameObject> list)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Removes any items from the `dictionary` that use destroyed objects as their key.</summary>
        public static void RemoveDestroyedObjects<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : Object
        {
        }

        /// <summary>
        /// Creates a new dictionary and returns true if it was null or calls <see cref="RemoveDestroyedObjects"/> and
        /// returns false if it wasn't.
        /// </summary>
        public static bool InitializeCleanDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary) where TKey : Object
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Context Menus
        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu function which passes the result of <see cref="CalculateEditorFadeDuration"/> into `startFade`.
        /// </summary>
        public static void AddFadeFunction(GenericMenu menu, string label, bool isEnabled, AnimancerNode node, Action<float> startFade)
        {
        }

        /// <summary>[Animancer Extension] [Editor-Only]
        /// Returns the duration of the `node`s current fade (if any), otherwise returns the `defaultDuration`.
        /// </summary>
        public static float CalculateEditorFadeDuration(this AnimancerNode node, float defaultDuration = 1)
            => node.FadeSpeed > 0 ? 1 / node.FadeSpeed : defaultDuration;

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu function to open a web page. If the `linkSuffix` starts with a '/' then it will be relative to
        /// the <see cref="Strings.DocsURLs.Documentation"/>.
        /// </summary>
        public static void AddDocumentationLink(GenericMenu menu, string label, string linkSuffix)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Is the <see cref="MenuCommand.context"/> editable?</summary>
        [MenuItem("CONTEXT/" + nameof(AnimationClip) + "/Toggle Looping", validate = true)]
        [MenuItem("CONTEXT/" + nameof(AnimationClip) + "/Toggle Legacy", validate = true)]
        private static bool ValidateEditable(MenuCommand command)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Toggles the <see cref="Motion.isLooping"/> flag between true and false.</summary>
        [MenuItem("CONTEXT/" + nameof(AnimationClip) + "/Toggle Looping")]
        private static void ToggleLooping(MenuCommand command)
        {
        }

        /// <summary>Sets the <see cref="Motion.isLooping"/> flag.</summary>
        public static void SetLooping(AnimationClip clip, bool looping)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Swaps the <see cref="AnimationClip.legacy"/> flag between true and false.</summary>
        [MenuItem("CONTEXT/" + nameof(AnimationClip) + "/Toggle Legacy")]
        private static void ToggleLegacy(MenuCommand command)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Animator.Rebind"/>.</summary>
        [MenuItem("CONTEXT/" + nameof(Animator) + "/Restore Bind Pose", priority = 110)]
        private static void RestoreBindPose(MenuCommand command)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Type Names
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, string>
            TypeNames = new Dictionary<Type, string>
            {
                { typeof(object), "object" },
                { typeof(void), "void" },
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(char), "char" },
                { typeof(string), "string" },
                { typeof(short), "short" },
                { typeof(int), "int" },
                { typeof(long), "long" },
                { typeof(ushort), "ushort" },
                { typeof(uint), "uint" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
            };

        private static readonly Dictionary<Type, string>
            FullTypeNames = new Dictionary<Type, string>(TypeNames);

        /************************************************************************************************************************/

        /// <summary>Returns the name of a `type` as it would appear in C# code.</summary>
        /// <remarks>Returned values are stored in a dictionary to speed up repeated use.</remarks>
        /// <example>
        /// <c>typeof(List&lt;float&gt;).FullName</c> would give you:
        /// <c>System.Collections.Generic.List`1[[System.Single, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]</c>
        /// <para></para>
        /// This method would instead return <c>System.Collections.Generic.List&lt;float&gt;</c> if `fullName` is <c>true</c>, or
        /// just <c>List&lt;float&gt;</c> if it is <c>false</c>.
        /// </example>
        public static string GetNameCS(this Type type, bool fullName = true)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Appends the generic arguments of `type` (after skipping the specified number).</summary>
        public static int AppendNameAndGenericArguments(StringBuilder text, Type type, bool fullName = true, int skipGenericArguments = 0)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Dummy Animancer Component
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// An <see cref="IAnimancerComponent"/> which is not actually a <see cref="Component"/>.
        /// </summary>
        public class DummyAnimancerComponent : IAnimancerComponent
        {
            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="DummyAnimancerComponent"/>.</summary>
            public DummyAnimancerComponent(Animator animator, AnimancerPlayable playable)
            {
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns true.</summary>
            public bool enabled => true;

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns the <see cref="Animator"/>'s <see cref="GameObject"/>.</summary>
            public GameObject gameObject => Animator.gameObject;

            /// <summary>[<see cref="IAnimancerComponent"/>] The target <see cref="UnityEngine.Animator"/>.</summary>
            public Animator Animator { get; set; }

            /// <summary>[<see cref="IAnimancerComponent"/>] The target <see cref="AnimancerPlayable"/>.</summary>
            public AnimancerPlayable Playable { get; private set; }

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns true.</summary>
            public bool IsPlayableInitialized => true;

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns false.</summary>
            public bool ResetOnDisable => false;

            /// <summary>[<see cref="IAnimancerComponent"/>] The <see cref="Animator.updateMode"/>.</summary>
            public AnimatorUpdateMode UpdateMode
            {
                get => Animator.updateMode;
                set => Animator.updateMode = value;
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns the `clip`.</summary>
            public object GetKey(AnimationClip clip) => clip;

            /************************************************************************************************************************/

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns null.</summary>
            public string AnimatorFieldName => null;

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns null.</summary>
            public string ActionOnDisableFieldName => null;

            /// <summary>[<see cref="IAnimancerComponent"/>] Returns the <see cref="Animator.updateMode"/> from when this object was created.</summary>
            public AnimatorUpdateMode? InitialUpdateMode { get; private set; }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

