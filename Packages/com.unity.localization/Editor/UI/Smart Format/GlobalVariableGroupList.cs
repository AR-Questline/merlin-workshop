using System;
using UnityEditor.Localization.UI.Toolkit;
using UnityEditor.UIElements;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class GlobalVariableGroupList : ReorderableList
    {
        public Type AddType { get; set; }

        public Func<Type, object> CreateInstance { get; set; }

        public GlobalVariableGroupList(SerializedProperty listProperty, Type managedType) :
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
            var element = list.ListProperty.GetArrayElementAtIndex(index);

            var nameField = new TextField("Variable Name");
            nameField.labelElement.style.minWidth = 120;
            nameField.RegisterValueChangedCallback(evt =>
            {
                // Variable must not contain any spaces or the smart format parser will not be able to correctly parse them as selectors
                nameField.SetValueWithoutNotify(evt.newValue.ReplaceWhiteSpaces("-"));
            });
            nameField.BindProperty(element.FindPropertyRelative("name"));
            root.Add(nameField);

            var variable = element.FindPropertyRelative("variable");
            var label = ManagedReferenceUtility.GetDisplayName(variable.managedReferenceFullTypename);

            var propEditor = new PropertyField(variable, label.text);
            propEditor.BindProperty(list.ListProperty.serializedObject);
            root.Add(propEditor);
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

        void AddManagedItem(ReorderableList list, Type type, int index)
        {
            var instance = Activator.CreateInstance(type);
            var element = list.ListProperty.InsertArrayElement(index);
            var variable = element.FindPropertyRelative("variable");
            variable.managedReferenceValue = instance;

            var name = element.FindPropertyRelative("name");
            name.stringValue = list.ListProperty.arraySize > 1 ? $"variable-{list.ListProperty.arraySize - 1}" : "variable";

            list.ListProperty.serializedObject.ApplyModifiedProperties();
            list.RefreshList();
        }
    }
}
