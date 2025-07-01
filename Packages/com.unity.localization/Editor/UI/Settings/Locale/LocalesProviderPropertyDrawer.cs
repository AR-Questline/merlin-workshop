using System;
using System.Collections.Generic;
using UnityEditor.Localization.UI.Toolkit;
using UnityEditor.UIElements;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocalesProvider))]
    class LocalesProviderPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = Resources.GetTemplate(nameof(LocalesProvider));

            var list = new ReorderableList(new List<Locale>());
            list.HeaderTitle = "Available Locales";
            list.HeaderIcon = EditorIcons.Locale;
            list.ReorderCallback = ChangeLocaleOrder;
            list.CreateItemCallback = CreateItem;
            list.RefreshListCallback = UpdateList;
            list.RemoveCallback = RemoveSelectedLocale;
            list.DisplayAddButton = false; // There is no UI Toolkit ObjectPicker in 2019.4
            root.Q("locales-list").Add(list);

            var openGenerator = root.Q<Button>("open-generator-button");
            openGenerator.clicked += LocaleGeneratorWindow.ShowWindow;

            var addCustomLocale = root.Q<Button>("add-custom-locale");
            addCustomLocale.clicked += CustomLocaleUIWindow.ShowWindow;

            var addAll = root.Q<Button>("add-all-button");
            addAll.clicked += () =>
            {
                var assets = AssetDatabase.FindAssets("t:Locale");
                for (int i = 0; i < assets.Length; ++i)
                {
                    LocalizationEditorSettings.AddLocale(AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(assets[i])), true);
                }
                list.RefreshList();
            };

            Action<Locale> projectLocaleAddedRemoved = l => list.RefreshList();
            EventHandler<Locale> projectLocaleSortChanged =  (a, b) => list.RefreshList();

            root.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                LocalizationEditorSettings.EditorEvents.LocaleSortOrderChanged += projectLocaleSortChanged;
                LocalizationEditorSettings.EditorEvents.LocaleAdded += projectLocaleAddedRemoved;
                LocalizationEditorSettings.EditorEvents.LocaleRemoved += projectLocaleAddedRemoved;
            });

            root.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationEditorSettings.EditorEvents.LocaleSortOrderChanged -= projectLocaleSortChanged;
                LocalizationEditorSettings.EditorEvents.LocaleAdded -= projectLocaleAddedRemoved;
                LocalizationEditorSettings.EditorEvents.LocaleRemoved -= projectLocaleAddedRemoved;
            });

            return root;
        }

        static void UpdateList(ReorderableList list)
        {
            list.List.Clear();
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                list.List.Add(locale);
            }
        }

        static void RemoveSelectedLocale(ReorderableList list, int index)
        {
            var locale = list.List[index] as Locale;
            if (locale == null)
                return;

            LocalizationEditorSettings.RemoveLocale(locale, true);
        }

        static void ChangeLocaleOrder(ReorderableList list, int from, int to)
        {
            var localesList = list.List as List<Locale>;

            var item = localesList[from];
            localesList.RemoveAt(from);
            localesList.Insert(to, item);

            // Reapply the sort orders
            Undo.RecordObjects(localesList.ToArray(), "Change locale sorting order");
            for (ushort i = 0; i < localesList.Count; ++i)
            {
                var locale = localesList[i];
                locale.SortOrder = i;
                EditorUtility.SetDirty(locale);
            }

            LocalizationEditorSettings.EditorEvents.RaiseLocaleSortOrderChanged(list, item);
        }

        static void CreateItem(ReorderableList list, int index, VisualElement root)
        {
            var locale = list.List[index] as Locale;

            var so = new SerializedObject(locale);
            var asset = Resources.GetTemplateAsset("LocalesProviderItem");
            asset.CloneTree(root);
            root.Query<TextField>().ForEach(tf => tf.isDelayed = true);
            root.Bind(so);
        }
    }
}
