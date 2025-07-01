// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A cache to optimize repeated attribute access.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AttributeCache_1
    /// 
    public static class AttributeCache<TAttribute> where TAttribute : class
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<MemberInfo, TAttribute>
            MemberToAttribute = new Dictionary<MemberInfo, TAttribute>();

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <typeparamref name="TAttribute"/> attribute on the specified `member` (if there is one).
        /// </summary>
        public static TAttribute GetAttribute(MemberInfo member)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <typeparamref name="TAttribute"/> attribute (if any) on the specified `type` or its
        /// <see cref="Type.BaseType"/> (recursively).
        /// </summary>
        public static TAttribute GetAttribute(Type type)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <typeparamref name="TAttribute"/> attribute on the specified `field` or its
        /// <see cref="FieldInfo.FieldType"/> or <see cref="MemberInfo.DeclaringType"/>.
        /// </summary>
        public static TAttribute FindAttribute(FieldInfo field)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Returns the <typeparamref name="TAttribute"/> attribute on the underlying field of the `property` or its
        /// <see cref="FieldInfo.FieldType"/> or <see cref="MemberInfo.DeclaringType"/> or any of the parent properties
        /// or the type of the <see cref="SerializedObject.targetObject"/>.
        /// </summary>
        public static TAttribute FindAttribute(SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/
    }
}

#endif

