using System;

namespace UnityEditor.Localization.UI
{
    static class TypeUtility
    {
        public static void PopulateMenuWithCreateItems(GenericMenu menu, Type baseType, Action<Type> selected, Type requiredAttribute = null)
        {
            var foundTypes = TypeCache.GetTypesDerivedFrom(baseType);
            for (int i = 0; i < foundTypes.Count; ++i)
            {
                var type = foundTypes[i];

                if (type.IsAbstract || type.IsGenericType)
                    continue;

                if (Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                    continue;

                if (requiredAttribute != null && !Attribute.IsDefined(type, requiredAttribute))
                    continue;

                // Ignore Unity Objects, they can not use SerializeReference
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    continue;

                var name = ManagedReferenceUtility.GetDisplayName(type);

                menu.AddItem(name, false, () =>
                {
                    selected(type);
                });
            }
        }
    }
}
