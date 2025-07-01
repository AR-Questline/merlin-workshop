using System;
using System.Linq;
using UnityEditor.Localization.UI.Toolkit;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.UIElements;
using ReorderableList = UnityEditor.Localization.UI.Toolkit.ReorderableList;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(SmartFormatter))]
    class SmartFormatterPropertyField : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = Resources.GetTemplate(nameof(SmartFormatter));
            root = root.Q<Foldout>();
            root.Bind(property.serializedObject);

            var sources = new ManagedReferenceReorderableList(property.FindPropertyRelative("m_Sources"), typeof(ISource));
            sources.HeaderTitle = "Sources";
            sources.HeaderTooltip = "Used to evaluate a selector. Checked in order of the list, top first.";
            sources.AddCallback = (l, i) => ShowAddMenu(l, i, property);
            root.Add(sources);

            var formatters = new ManagedReferenceReorderableList(property.FindPropertyRelative("m_Formatters"), typeof(IFormatter));
            formatters.HeaderTitle = "Formatters";
            formatters.HeaderTooltip = "Used to convert an object to a string. Checked in order of the list, top first.";
            formatters.AddCallback = (l, i) => ShowAddMenu(l, i, property);
            root.Add(formatters);

            return root;
        }

        void ShowAddMenu(ReorderableList list, int index, SerializedProperty property)
        {
            var managedList = list as ManagedReferenceReorderableList;

            var settings = list.ListProperty.serializedObject.targetObject as LocalizationSettings;
            SmartFormatter smartFormatterInstance = null;
            if (settings != null)
            {
                smartFormatterInstance = settings.GetStringDatabase()?.SmartFormatter;
            }
            else
            {
                smartFormatterInstance = property.GetActualObjectForSerializedProperty<SmartFormatter>(fieldInfo);
            }
            Debug.Assert(smartFormatterInstance != null, $"Failed to extract {nameof(SmartFormatter)} instance.");

            var menu = new GenericMenu();
            TypeUtility.PopulateMenuWithCreateItems(menu, managedList.AddType, type =>
            {
                // We only support 2 types of constructor. A default and one that takes a SmartFormatter.
                var hasDefaultConstructor = type.GetConstructors().Any(c => c.GetParameters().Length == 0);
                var hasSmartFormatterConstructor = type.GetConstructors().Any(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(SmartFormatter));

                if (hasSmartFormatterConstructor || hasDefaultConstructor)
                {
                    var elementProp = list.ListProperty.InsertArrayElement(index);
                    elementProp.managedReferenceValue = hasDefaultConstructor ? Activator.CreateInstance(type) : Activator.CreateInstance(type, smartFormatterInstance);
                    list.ListProperty.serializedObject.ApplyModifiedProperties();
                    list.RefreshList();
                }
                else
                {
                    Debug.LogWarning($"Can not create an instance of {type}, it does not have a default constructor or a constructor that takes a SmartFormatter parameter.");
                }
            });

            menu.ShowAsContext();
        }
    }
}
