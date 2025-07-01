// Animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// A button that allows the user to select an object type for a [<see cref="SerializeReference"/>] field.</summary>
    /// <example><code>
    /// public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
    /// {
    ///     using (new TypeSelectionButton(area, property, label, true))
    ///     {
    ///         EditorGUI.PropertyField(area, property, label, true);
    ///     }
    /// }
    /// </code></example>
    public readonly struct TypeSelectionButton : IDisposable
    {
        /************************************************************************************************************************/

        /// <summary>The pixel area occupied by the button.</summary>
        public readonly Rect Area;

        /// <summary>The <see cref="SerializedProperty"/> representing the attributed field.</summary>
        public readonly SerializedProperty Property;

        /// <summary>The original <see cref="Event.type"/> from when this button was initialized.</summary>
        public readonly EventType EventType;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TypeSelectionButton"/>.</summary>
        public TypeSelectionButton(Rect area, SerializedProperty property, bool hasLabel) : this()
        {
        }

        /************************************************************************************************************************/

        void IDisposable.Dispose() => DoGUI();

        /// <summary>Draws this button's GUI.</summary>
        /// <remarks>Run this method after drawing the target property so the button draws on top of its label.</remarks>
        public void DoGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Shows a menu to select which type of object to assign to the `property`.</summary>
        private void ShowTypeSelectorMenu(SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds a menu function to assign a new instance of the `newType` to the `property`.</summary>
        private static void AddTypeSelector(
            GenericMenu menu, SerializedProperty property, Type fieldType, Type selectedType, Type newType)
        {
        }

        /************************************************************************************************************************/

        private const string
            PrefKeyPrefix = nameof(TypeSelectionButton) + ".",
            PrefMenuPrefix = "Display Options/";

        private static readonly BoolPref
            UseFullNames = new BoolPref(PrefKeyPrefix + nameof(UseFullNames), PrefMenuPrefix + "Show Full Names", false),
            UseTypeHierarchy = new BoolPref(PrefKeyPrefix + nameof(UseTypeHierarchy), PrefMenuPrefix + "Show Type Hierarchy", false);

        private static string GetSelectorLabel(Type fieldType, Type newType)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static readonly List<Type>
            AllTypes = new List<Type>(1024);
        private static readonly Dictionary<Type, List<Type>>
            TypeToDerived = new Dictionary<Type, List<Type>>();

        /// <summary>Returns a list of all types that inherit from the `baseType`.</summary>
        public static List<Type> GetDerivedTypes(Type baseType)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Is the `type` supported by <see cref="SerializeReference"/> fields?</summary>
        public static bool IsViableType(Type type) =>
            !type.IsAbstract &&
            !type.IsEnum &&
            !type.IsGenericTypeDefinition &&
            !type.IsInterface &&
            !type.IsPrimitive &&
            !type.IsSpecialName &&
            type.Name[0] != '<' &&
            type.IsDefined(typeof(SerializableAttribute), false) &&
            !type.IsDefined(typeof(ObsoleteAttribute), true) &&
            !typeof(Object).IsAssignableFrom(type) &&
            type.GetConstructor(AnimancerEditorUtilities.InstanceBindings, null, Type.EmptyTypes, null) != null;

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new instance of the `type` using its parameterless constructor if it has one or a fully
        /// uninitialized object if it doesn't. Or returns <c>null</c> if the <see cref="Type.IsAbstract"/>.
        /// </summary>
        public static object CreateDefaultInstance(Type type)
        {
            return default;
        }

        /// <summary>
        /// Creates a <typeparamref name="T"/> using its parameterless constructor if it has one or a fully
        /// uninitialized object if it doesn't. Or returns <c>null</c> if the <see cref="Type.IsAbstract"/>.
        /// </summary>
        public static T CreateDefaultInstance<T>() => (T)CreateDefaultInstance(typeof(T));

        /************************************************************************************************************************/

        /// <summary>
        /// Copies the values of all fields in `from` into corresponding fields in `to` as long as they have the same
        /// name and compatible types.
        /// </summary>
        public static void CopyCommonFields(object from, object to)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif
