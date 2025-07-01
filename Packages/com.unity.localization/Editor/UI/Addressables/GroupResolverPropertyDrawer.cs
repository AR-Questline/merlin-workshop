using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;
using UnityEditor.Localization.UI.Toolkit;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI.Addressables
{
    [CustomPropertyDrawer(typeof(GroupResolver))]
    class GroupResolverPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout {value = property.isExpanded, text = property.displayName};
            root.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);

            var name = new TextField("Shared Group Name");
            name.bindingPath = "m_SharedGroupName";
            root.Add(name);

            var group = new ObjectField("Shared Group") { allowSceneObjects = false, objectType = typeof(AddressableAssetGroup) };
            var groupProperty = property.FindPropertyRelative("m_SharedGroup");
            group.bindingPath = "m_SharedGroup";
            group.RegisterValueChangedCallback(evt => name.style.display = evt.newValue != null ? DisplayStyle.None : DisplayStyle.Flex);
            name.style.display = groupProperty.objectReferenceValue != null ? DisplayStyle.None : DisplayStyle.Flex;
            root.Add(group);

            var localeName = new TextField("Locale Group Name");
            localeName.bindingPath = "m_LocaleGroupNamePattern";
            root.Add(localeName);

            var localeGroupsProperty = property.FindPropertyRelative("m_LocaleGroups");
            var list = new ReorderableList(localeGroupsProperty)
            {
                CreateItemCallback = CreateItem,
                AddCallback = ShowAddItemMenu,
                RemoveCallback = RemoveItem,
                ReorderCallback = MoveItem
            };
            root.Add(list);

            var readOnly = new Toggle("Read Only");
            readOnly.bindingPath = "m_MarkEntriesReadOnly";
            root.Add(readOnly);

            return root;
        }

        static void CreateItem(ReorderableList list, int index, VisualElement root)
        {
            var element = list.ListProperty.GetArrayElementAtIndex(index);
            var codeProperty = element.FindPropertyRelative("localeIdentifier.m_Code");
            var locale = LocalizationEditorSettings.GetLocale(codeProperty.stringValue);
            var label = locale != null ? locale.ToString() : codeProperty.stringValue;
            var group = new ObjectField(label) {allowSceneObjects = false, objectType = typeof(AddressableAssetGroup)};
            group.BindProperty(element.FindPropertyRelative("group"));
            root.Add(group);
        }

        static void RemoveItem(ReorderableList list, int index)
        {
            list.ListProperty.DeleteArrayElementAtIndex(index);
            list.ListProperty.serializedObject.ApplyModifiedProperties();
        }

        static void MoveItem(ReorderableList list, int from, int to)
        {
            list.ListProperty.MoveArrayElement(from, to);
            list.ListProperty.serializedObject.ApplyModifiedProperties();
        }

        static void ShowAddItemMenu(ReorderableList list, int index)
        {
            var menu = new GenericMenu();

            var hashSet = new HashSet<string>();
            for (int i = 0; i < list.ListProperty.arraySize; ++i)
            {
                var element = list.ListProperty.GetArrayElementAtIndex(i);
                var codeProperty = element.FindPropertyRelative("localeIdentifier.m_Code");
                hashSet.Add(codeProperty.stringValue);
            }

            var locales = LocalizationEditorSettings.GetLocales();
            foreach (var locale in locales)
            {
                if (!hashSet.Contains(locale.Identifier.Code))
                {
                    menu.AddItem(new GUIContent(locale.ToString()), false, () =>
                    {
                        var element = list.ListProperty.InsertArrayElement(index);
                        var codeProperty = element.FindPropertyRelative("localeIdentifier.m_Code");
                        codeProperty.stringValue = locale.Identifier.Code;
                        list.ListProperty.serializedObject.ApplyModifiedProperties();
                        list.RefreshList();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(locale.ToString()), true);
                }
            }

            menu.ShowAsContext();
        }
    }
}
