using System;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Localization.UI
{
    class ReorderableListExtended : ReorderableList
    {
        Type m_AddType;

        /// <summary>
        /// The label displayed for the whole list.
        /// </summary>
        public GUIContent Header
        {
            set
            {
                drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, value);
            }
        }

        /// <summary>
        /// Use to populate the Add item button. All non abstract classes that derive from this will be shown.
        /// </summary>
        public Type AddMenuType
        {
            set
            {
                m_AddType = value;
                onAddDropdownCallback = ShowAddMenu;
            }
        }

        /// <summary>
        /// Called before showing the menu so that additional menu items can be added.
        /// </summary>
        public Action<GenericMenu> AddMenuItems { get; set; }

        /// <summary>
        /// Called to create a new instance of the object when it is about to be added to the list.
        /// By default it will just use the default constructor however you may wish to use a custom one or do some initialization.
        /// </summary>
        public Func<Type, object> CreateNewInstance { get; set; } = (t) => Activator.CreateInstance(t);

        /// <summary>
        /// If set then when populating the list of types that can be added they will also need to include this attribute.
        /// </summary>
        public Type RequiredAttribute { get; set; }

        /// <summary>
        /// Displayed when no items are available to add.
        /// </summary>
        public GUIContent NoItemMenuItem { get; set; }

        public ReorderableListExtended(SerializedObject serializedObject, SerializedProperty elements) :
            base(serializedObject, elements)
        {
            drawElementCallback = DrawElement;
            elementHeightCallback = GetElementHeight;
            onCanRemoveCallback = CanRemoveElement;
        }

        public ReorderableListExtended(SerializedObject serializedObject, SerializedProperty elements,  bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton) :
            base(serializedObject, elements, draggable, displayHeader, displayAddButton, displayRemoveButton)
        {
            drawElementCallback = DrawElement;
            elementHeightCallback = GetElementHeight;
            onCanRemoveCallback = CanRemoveElement;
        }

        void DrawElement(Rect rect, int idx, bool isActive, bool isFocused)
        {
            var element = serializedProperty.GetArrayElementAtIndex(idx);

            bool disabled = IsReadOnly(element);

            EditorGUI.BeginDisabledGroup(disabled);

            var label = ManagedReferenceUtility.GetDisplayName(element.managedReferenceFullTypename);
            rect.xMin += 8; // Prevent the foldout arrow(>) being drawn over the reorder icon(=) when showing LocalizationSettings in the inspector.
            EditorGUI.PropertyField(rect, element, label, true);
            EditorGUI.EndDisabledGroup();
        }

        bool CanRemoveElement(ReorderableList list)
        {
            var prop = serializedProperty.GetArrayElementAtIndex(list.index);
            return !IsReadOnly(prop);
        }

        float GetElementHeight(int idx)
        {
            var element = serializedProperty.GetArrayElementAtIndex(idx);
            return EditorGUI.GetPropertyHeight(element, true);
        }

        bool IsReadOnly(SerializedProperty prop)
        {
            bool isReadOnly = false;
            if (prop.propertyType == SerializedPropertyType.ManagedReference && !string.IsNullOrEmpty(prop.managedReferenceFullTypename))
            {
                var type = ManagedReferenceUtility.GetType(prop.managedReferenceFullTypename);
                if (type != null)
                {
                    isReadOnly = Attribute.IsDefined(type, typeof(HideInInspector));
                }
            }
            return isReadOnly;
        }

        void ShowAddMenu(Rect rect, ReorderableList lst)
        {
            var menu = new GenericMenu();
            TypeUtility.PopulateMenuWithCreateItems(menu, m_AddType, type =>
            {
                var element = serializedProperty.AddArrayElement();
                element.managedReferenceValue = CreateNewInstance(type);
                serializedProperty.serializedObject.ApplyModifiedProperties();
            }, RequiredAttribute);

            AddMenuItems?.Invoke(menu);

            if (menu.GetItemCount() == 0 && NoItemMenuItem != null)
                menu.AddDisabledItem(NoItemMenuItem);

            menu.ShowAsContext();
        }
    }
}
