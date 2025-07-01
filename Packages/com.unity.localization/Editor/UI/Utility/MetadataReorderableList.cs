using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace UnityEditor.Localization.UI.Toolkit
{
    internal class MetadataReorderableList : ManagedReferenceReorderableList
    {
        MetadataTypeAttribute m_MetadataType;

        public MetadataReorderableList(SerializedProperty property, MetadataTypeAttribute metadataType) :
            base(property, typeof(IMetadata))
        {
            m_MetadataType = metadataType;
            AddCallback = ShowAddItemMenu;
        }

        public static void PopulateAddMetadataMenu(GenericMenu menu, MetadataType allowedType, SerializedProperty listProperty, IList<Type> metadataTypes, Action<Type> addCallback)
        {
            for (int i = 0; i < metadataTypes.Count; ++i)
            {
                var md = metadataTypes[i];
                if (md.IsAbstract)
                    continue;

                var itemAttribute = md.GetCustomAttribute<MetadataAttribute>();
                if (itemAttribute == null)
                    continue;

                if ((itemAttribute.AllowedTypes & allowedType) == 0)
                    continue;

                bool enabled = true;
                if (!itemAttribute.AllowMultiple)
                {
                    for (int j = 0; j < listProperty.arraySize; ++j)
                    {
                        var typeName = listProperty.GetArrayElementAtIndex(j).managedReferenceFullTypename;
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            var type = ManagedReferenceUtility.GetType(listProperty.GetArrayElementAtIndex(j).managedReferenceFullTypename);
                            if (type == md)
                            {
                                enabled = false;
                                break;
                            }
                        }
                    }
                }

                var name = itemAttribute.MenuItem;
                if (string.IsNullOrEmpty(name))
                    name = ObjectNames.NicifyVariableName(md.Name);

                var label = new GUIContent(name);
                if (enabled)
                {
                    menu.AddItem(label, false, () =>
                    {
                        addCallback(md);
                    });
                }
                else
                {
                    menu.AddDisabledItem(label);
                }
            }
        }

        void ShowAddItemMenu(ReorderableList list, int index)
        {
            var menu = new GenericMenu();
            PopulateAddMetadataMenu(menu, m_MetadataType.Type, list.ListProperty, TypeCache.GetTypesDerivedFrom<IMetadata>(), (type) => AddManagedItem(list, type, index));
            menu.ShowAsContext();
        }
    }
}
