using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sirenix.Utilities
{
    public static class MemberInfoExtensions
    {
        public static T GetAttribute<T>(this ICustomAttributeProvider member, bool inherit = false) where T : Attribute
        {
            return default;
        }

        public static IEnumerable<T> GetAttributes<T>(this ICustomAttributeProvider member, bool inherit = false) where T : Attribute
        {
            return default;
        }
    }
}