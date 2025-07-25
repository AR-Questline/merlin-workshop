// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// Interface for components to indicate which <see cref="GameObject"/> is the root of a character when
    /// <see cref="Editor.AnimancerEditorUtilities.FindRoot(GameObject)"/> is called.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/ICharacterRoot
    /// 
    public interface ICharacterRoot
    {
        /************************************************************************************************************************/
#pragma warning disable IDE1006 // Naming Styles.
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Transform"/> to search for <see cref="AnimationClip"/>s beneath.
        /// </summary>
        ///
        /// <example>
        /// Implementing this interface in a <see cref="MonoBehaviour"/> will automatically inherit this property so
        /// you do not need to do anything else:
        /// <para></para><code>
        /// public class MyComponent : MonoBehaviour, IAnimancerRoot
        /// {
        /// }
        /// </code>
        /// But if you want to have your script point to a different object as the root, you can explicitly implement
        /// this property:
        /// <para></para><code>
        /// public class MyComponent : MonoBehaviour, IAnimancerRoot
        /// {
        ///     Transform IAnimancerRoot.transform => ???;
        /// }
        /// </code></example>
        Transform transform { get; }

        /************************************************************************************************************************/
#pragma warning restore IDE1006 // Naming Styles.
        /************************************************************************************************************************/
    }
}

/************************************************************************************************************************/
#if UNITY_EDITOR
/************************************************************************************************************************/

namespace Animancer.Editor
{
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerEditorUtilities
    partial class AnimancerEditorUtilities
    {
        /************************************************************************************************************************/

        /// <summary>Takes a `gameObject` and returns the root <see cref="Transform"/> of the character it is part of.</summary>
        /// 
        /// <remarks>
        /// This method first searches all parents for an <see cref="ICharacterRoot"/>. If it finds one, it returns the
        /// <see cref="ICharacterRoot.transform"/>.
        /// <para></para>
        /// Otherwise, if the object is part of a prefab then it returns the root of that prefab instance.
        /// <para></para>
        /// Otherwise, it counts the number of Animators in the children of the `gameObject` then does
        /// the same for each parent. If it finds a parent with a different number of child Animators, it
        /// assumes that object is the parent of multiple characters and returns the previous parent as the root.
        /// </remarks>
        ///
        /// <example>
        /// <h2>Simple Hierarchy</h2>
        /// <code>
        /// - Character - Rigidbody, etc.
        ///     - Model - Animator, AnimancerComponent
        ///     - States - Various components which reference the AnimationClips they will play
        /// </code>
        /// Passing the <c>Model</c> into this method will return the <c>Character</c> because it has the same
        /// number of Animator components in its children.
        ///
        /// <h2>Shared Hierarchy</h2>
        /// <code>
        /// - Characters - Empty object used to group all characters
        ///     - Character - Rigidbody, etc.
        ///         - Model - Animator, AnimancerComponent
        ///         - States - Various components which reference the AnimationClips they will play
        ///     - Another Character
        ///         - Model
        ///         - States
        /// </code>
        /// <list type="bullet">
        /// <item><c>Model</c> has one Animator and no more in its children.</item>
        /// <item>And <c>Character</c> has one Animator in its children (the same one).</item>
        /// <item>But <c>Characters</c> has two Animators in its children (one on each character).</item>
        /// </list>
        /// So it picks the <c>Character</c> as the root.
        ///
        /// <h2>Complex Hierarchy</h2>
        /// <code>
        /// - Character - Rigidbody, etc.
        ///     - Model - Animator, AnimancerComponent
        ///     - States - Various components which reference the AnimationClips they will play
        ///     - Another Model - Animator (maybe the character is holding a gun which has a reload animation)
        /// </code>
        /// In this case, the automatic system would see that the <c>Character</c> already has more child
        /// <see cref="Animator"/>s than the selected <c>Model</c> so it would only return the <c>Model</c> itself.
        /// This can be fixed by making any of the scripts on the <c>Character</c> implement <see cref="ICharacterRoot"/>
        /// to tell the system which object you want it to use as the root.
        /// </example>
        public static Transform FindRoot(GameObject gameObject)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="FindRoot(GameObject)"/> if the specified `obj` is a <see cref="GameObject"/> or
        /// <see cref="Component"/>.
        /// </summary>
        public static Transform FindRoot(Object obj)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Outputs the <see cref="GameObject"/> assignated with the `obj` and returns true if it exists.</summary>
        /// <remarks>
        /// If the `obj` is a <see cref="GameObject"/> it is used as the result.
        /// <para></para>
        /// Or if the `obj` is a <see cref="Component"/> then its <see cref="Component.gameObject"/> is used as the result.
        /// </remarks>
        public static bool TryGetGameObject(Object obj, out GameObject gameObject)
        {
            gameObject = default(GameObject);
            return default;
        }

        /************************************************************************************************************************/
    }
}

/************************************************************************************************************************/
#endif
/************************************************************************************************************************/

