using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI.Toolkit
{
    class ManagedReferenceReorderableList : ReorderableList
    {
        public Type AddType { get; set; }

        public Func<Type, object> CreateInstance { get; set; }

        public ManagedReferenceReorderableList(SerializedProperty listProperty, Type managedType) :
            base(listProperty)
        {
            CreateItemCallback = CreateManagedItem;
            AddCallback = ShowAddItemMenu;
            RemoveCallback = RemoveItem;
            ReorderCallback = SwapItems;
            AddType = managedType;
        }

        static void CreateManagedItem(ReorderableList list, int index, VisualElement root)
        {
            const float leftMargin = 14; // Space for foldout arrow

            var element = list.ListProperty.GetArrayElementAtIndex(index);

            var label = ManagedReferenceUtility.GetDisplayName(element.managedReferenceFullTypename);
            if (!element.hasVisibleChildren)
            {
                root.Add(new Label(label.text){ style = { paddingLeft = leftMargin } });
            }
            else
            {
                var propEditor = new PropertyField(element, label.text) { style = {paddingLeft = leftMargin } };
                propEditor.BindProperty(list.ListProperty.serializedObject);
                propEditor.SetEnabled(!IsReadOnly(element));

                propEditor.style.paddingLeft = 14;

                root.Add(propEditor);
            }
        }

        static void RemoveItem(ReorderableList list, int index)
        {
            list.ListProperty.DeleteArrayElementAtIndex(list.Selected);
            list.ListProperty.serializedObject.ApplyModifiedProperties();

            // The bindings will have changed
            list.RefreshList();
        }

        static void SwapItems(ReorderableList list, int from, int to)
        {
            list.ListProperty.MoveArrayElement(from, to);
            list.ListProperty.serializedObject.ApplyModifiedProperties();
        }

        void ShowAddItemMenu(ReorderableList list, int index)
        {
            GenericMenu menu = new GenericMenu();

            Type last = null;
            var foundTypes = TypeCache.GetTypesDerivedFrom(AddType);
            for (int i = 0; i < foundTypes.Count; ++i)
            {
                var type = foundTypes[i];

                if (type.IsAbstract || type.IsGenericType)
                    continue;

                // Ignore Unity types as they can not be managed references.
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    continue;

                last = type;

                var name = ManagedReferenceUtility.GetDisplayName(type);
                menu.AddItem(name, false, () =>
                {
                    AddManagedItem(list, type, index);
                });
            }

            if (menu.GetItemCount() == 1)
            {
                AddManagedItem(list, last, index);
            }
            else
            {
                menu.ShowAsContext();
            }
        }

        protected void AddManagedItem(ReorderableList list, Type type, int index)
        {
            var instance = CreateInstance != null ? CreateInstance(type) : Activator.CreateInstance(type);;
            var element = list.ListProperty.InsertArrayElement(index);
            element.managedReferenceValue = instance;
            list.ListProperty.serializedObject.ApplyModifiedProperties();
            list.RefreshList();
        }

        static bool IsReadOnly(SerializedProperty prop)
        {
            bool isReadOnly = false;
            if (!string.IsNullOrEmpty(prop.managedReferenceFullTypename))
            {
                var type = ManagedReferenceUtility.GetType(prop.managedReferenceFullTypename);
                if (type != null)
                {
                    isReadOnly = Attribute.IsDefined(type, typeof(HideInInspector));
                }
            }
            return isReadOnly;
        }
    }
}
