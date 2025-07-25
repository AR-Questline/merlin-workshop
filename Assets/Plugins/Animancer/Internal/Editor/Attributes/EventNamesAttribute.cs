// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;

#if UNITY_EDITOR
using Animancer.Editor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections;
#endif

namespace Animancer
{
    /// <summary>[Editor-Conditional]
    /// Specifies a set of acceptable names for <see cref="AnimancerEvent"/>s so they can be displayed using a dropdown
    /// menu instead of a text field.
    /// </summary>
    /// 
    /// <remarks>
    /// Placing this attribute on a type applies it to all fields in that type.
    /// <para></para>
    /// Note that values selected using the dropdown menu are still stored as strings. Modifying the names in the
    /// script will NOT automatically update any values previously set in the Inspector.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer/usage#event-names">Event Names</see>
    /// </remarks>
    /// 
    /// <example><code>
    /// [EventNames(...)]// Apply to all fields in this class.
    /// public class AttackState
    /// {
    ///     [SerializeField]
    ///     [EventNames(...)]// Apply to only this field.
    ///     private ClipTransition _Action;
    /// }
    /// </code>
    /// See the constructors for examples of their usage.
    /// </example>
    /// 
    /// https://kybernetik.com.au/animancer/api/Animancer/EventNamesAttribute
    /// 
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    [System.Diagnostics.Conditional(Strings.UnityEditor)]
    public sealed class EventNamesAttribute : Attribute
    {
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The names that can be used for events in the attributed field.</summary>
        public readonly string[] Names;
#endif

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="EventNamesAttribute"/> containing the specified `names`.</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException">`names` contains no elements.</exception>
        /// <example><code>
        /// public class AttackState
        /// {
        ///     [SerializeField]
        ///     [EventNames("Hit Start", "Hit End")]
        ///     private ClipTransition _Animation;
        /// 
        ///     private void Awake()
        ///     {
        ///         _Animation.Events.SetCallback("Hit Start", OnHitStart);
        ///         _Animation.Events.SetCallback("Hit End", OnHitEnd);
        ///     }
        /// 
        ///     private void OnHitStart() { }
        ///     private void OnHitEnd() { }
        /// }
        /// </code></example>
        public EventNamesAttribute(params string[] names)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="EventNamesAttribute"/> with <see cref="Names"/> from the `type`.</summary>
        /// 
        /// <remarks>
        /// If the `type` is an enum, all of its values will be used.
        /// <para></para>
        /// Otherwise the values of all static <see cref="string"/> fields (including constants) will be used.
        /// </remarks>
        /// <exception cref="ArgumentNullException"/>
        /// 
        /// <example><code>
        /// public class AttackState
        /// {
        ///     public static class Events
        ///     {
        ///         public const string HitStart = "Hit Start";
        ///         public const string HitEnd = "Hit End";
        ///     }
        /// 
        ///     [SerializeField]
        ///     [EventNames(typeof(Events))]// Use all string fields in the Events class.
        ///     private ClipTransition _Animation;
        /// 
        ///     private void Awake()
        ///     {
        ///         _Animation.Events.SetCallback(Events.HitStart, OnHitStart);
        ///         _Animation.Events.SetCallback(Events.HitEnd, OnHitEnd);
        ///     }
        /// 
        ///     private void OnHitStart() { }
        ///     private void OnHitEnd() { }
        /// }
        /// </code></example>
        public EventNamesAttribute(Type type)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="EventNamesAttribute"/> with <see cref="Names"/> from a member in the `type`
        /// with the specified `name`.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException">No member with the specified `name` exists in the `type`.</exception>
        /// 
        /// <remarks>
        /// The specified member must be static and can be a Field, Property, or Method.
        /// <para></para>
        /// The member type can be anything implementing <see cref="IEnumerable"/> (including arrays, lists, and
        /// coroutines).
        /// </remarks>
        /// 
        /// <example><code>
        /// public class AttackState
        /// {
        ///     public static readonly string[] Events = { "Hit Start", "Hit End" };
        /// 
        ///     [SerializeField]
        ///     [EventNames(typeof(AttackState), nameof(Events))]// Get the names from AttackState.Events.
        ///     private ClipTransition _Animation;
        /// 
        ///     private void Awake()
        ///     {
        ///         _Animation.Events.SetCallback(Events[0], OnHitStart);
        ///         _Animation.Events.SetCallback(Events[1], OnHitEnd);
        ///     }
        /// 
        ///     private void OnHitStart() { }
        ///     private void OnHitEnd() { }
        /// }
        /// </code></example>
        public EventNamesAttribute(Type type, string name)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>The entry used for the menu function to clear the name (U+202F Narrow No-Break Space).</summary>
        public const string NoName = " ";

        /************************************************************************************************************************/

        private static string[] AddSpecialItems(string[] names)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static string[] GatherNamesFromStaticFields(Type type)
        {
            return default;
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

