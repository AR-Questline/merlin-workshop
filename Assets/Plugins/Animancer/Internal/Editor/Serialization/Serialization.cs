// Serialization // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Shared File Last Modified: 2023-02-24
namespace Animancer.Editor
// namespace InspectorGadgets.Editor
// namespace UltEvents.Editor
{
    /// <summary>The possible states for a function in a <see cref="GenericMenu"/>.</summary>
    public enum MenuFunctionState
    {
        /************************************************************************************************************************/

        /// <summary>Displayed normally.</summary>
        Normal,

        /// <summary>Has a check mark next to it to show that it is selected.</summary>
        Selected,

        /// <summary>Greyed out and unusable.</summary>
        Disabled,

        /************************************************************************************************************************/
    }

    /// <summary>[Editor-Only] Various serialization utilities.</summary>
    public static partial class Serialization
    {
        /************************************************************************************************************************/
        #region Public Static API
        /************************************************************************************************************************/

        /// <summary>The text used in a <see cref="SerializedProperty.propertyPath"/> to denote array elements.</summary>
        public const string
            ArrayDataPrefix = ".Array.data[",
            ArrayDataSuffix = "]";

        /// <summary>Bindings for Public and Non-Public Instance members.</summary>
        public const BindingFlags
            InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /************************************************************************************************************************/

        /// <summary>Returns a user friendly version of the <see cref="SerializedProperty.propertyPath"/>.</summary>
        public static string GetFriendlyPath(this SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Get Value
        /************************************************************************************************************************/

        /// <summary>Gets the value of the specified <see cref="SerializedProperty"/>.</summary>
        public static object GetValue(this SerializedProperty property, object targetObject)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Gets the value of the <see cref="SerializedProperty"/>.</summary>
        public static object GetValue(this SerializedProperty property) => GetValue(property, property.serializedObject.targetObject);

        /// <summary>Gets the value of the <see cref="SerializedProperty"/>.</summary>
        public static T GetValue<T>(this SerializedProperty property) => (T)GetValue(property);

        /// <summary>Gets the value of the <see cref="SerializedProperty"/>.</summary>
        public static void GetValue<T>(this SerializedProperty property, out T value) => value = (T)GetValue(property);

        /************************************************************************************************************************/

        /// <summary>Gets the value of the <see cref="SerializedProperty"/> for each of its target objects.</summary>
        public static T[] GetValues<T>(this SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Is the value of the `property` the same as the default serialized value for its type?</summary>
        public static bool IsDefaultValueByType(SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Set Value
        /************************************************************************************************************************/

        /// <summary>Sets the value of the specified <see cref="SerializedProperty"/>.</summary>
        public static void SetValue(this SerializedProperty property, object targetObject, object value)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Sets the value of the <see cref="SerializedProperty"/>.</summary>
        public static void SetValue(this SerializedProperty property, object value)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Resets the value of the <see cref="SerializedProperty"/> to the default value of its type and all its field
        /// types, ignoring values set by constructors or field initializers.
        /// </summary>
        /// <remarks>
        /// If you want to run constructors and field initializers, you can call
        /// <see cref="PropertyAccessor.ResetValue"/> instead.
        /// </remarks>
        public static void ResetValue(SerializedProperty property, string undoName = "Inspector")
        {
        }

        /************************************************************************************************************************/

        /// <summary>Copies the value of `from` into `to` (including all nested properties).</summary>
        public static float CopyValueFrom(this SerializedProperty to, SerializedProperty from)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Gradients
        /************************************************************************************************************************/

        private static PropertyInfo _GradientValue;

        /// <summary><c>SerializedProperty.gradientValue</c> is internal.</summary>
        private static PropertyInfo GradientValue
        {
            get
            {
                if (_GradientValue == null)
                    _GradientValue = typeof(SerializedProperty).GetProperty("gradientValue", InstanceBindings);

                return _GradientValue;
            }
        }

        /// <summary>Gets the <see cref="Gradient"/> value from a <see cref="SerializedPropertyType.Gradient"/>.</summary>
        public static Gradient GetGradientValue(this SerializedProperty property) => (Gradient)GradientValue.GetValue(property, null);

        /// <summary>Sets the <see cref="Gradient"/> value on a <see cref="SerializedPropertyType.Gradient"/>.</summary>
        public static void SetGradientValue(this SerializedProperty property, Gradient value) => GradientValue.SetValue(property, value, null);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Indicates whether both properties refer to the same underlying field.</summary>
        public static bool AreSameProperty(SerializedProperty a, SerializedProperty b)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Executes the `action` once with a new <see cref="SerializedProperty"/> for each of the
        /// <see cref="SerializedObject.targetObjects"/>. Or if there is only one target, it uses the `property`.
        /// </summary>
        public static void ForEachTarget(this SerializedProperty property, Action<SerializedProperty> function,
            string undoName = "Inspector")
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a menu item to execute the specified `function` for each of the `property`s target objects.
        /// </summary>
        public static void AddFunction(this GenericMenu menu, string label, MenuFunctionState state, GenericMenu.MenuFunction function)
        {
        }

        /// <summary>
        /// Adds a menu item to execute the specified `function` for each of the `property`s target objects.
        /// </summary>
        public static void AddFunction(this GenericMenu menu, string label, bool enabled, GenericMenu.MenuFunction function)
            => AddFunction(menu, label, enabled ? MenuFunctionState.Normal : MenuFunctionState.Disabled, function);

        /************************************************************************************************************************/

        /// <summary>Adds a menu item to execute the specified `function` for each of the `property`s target objects.</summary>
        public static void AddPropertyModifierFunction(this GenericMenu menu, SerializedProperty property, string label,
            MenuFunctionState state, Action<SerializedProperty> function)
        {
        }

        /// <summary>Adds a menu item to execute the specified `function` for each of the `property`s target objects.</summary>
        public static void AddPropertyModifierFunction(this GenericMenu menu, SerializedProperty property, string label, bool enabled,
            Action<SerializedProperty> function)
            => AddPropertyModifierFunction(menu, property, label, enabled ? MenuFunctionState.Normal : MenuFunctionState.Disabled, function);

        /// <summary>Adds a menu item to execute the specified `function` for each of the `property`s target objects.</summary>
        public static void AddPropertyModifierFunction(this GenericMenu menu, SerializedProperty property, string label,
            Action<SerializedProperty> function)
            => AddPropertyModifierFunction(menu, property, label, MenuFunctionState.Normal, function);

        /************************************************************************************************************************/

        /// <summary>
        /// Calls the specified `method` for each of the underlying values of the `property` (in case it represents
        /// multiple selected objects) and records an undo step for any modifications made.
        /// </summary>
        public static void ModifyValues<T>(this SerializedProperty property, Action<T> method, string undoName = "Inspector")
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Records the state of the specified `property` so it can be undone.
        /// </summary>
        public static void RecordUndo(this SerializedProperty property, string undoName = "Inspector")
            => Undo.RecordObjects(property.serializedObject.targetObjects, undoName);

        /************************************************************************************************************************/

        /// <summary>
        /// Updates the specified `property` and marks its target objects as dirty so any changes to a prefab will be saved.
        /// </summary>
        public static void OnPropertyChanged(this SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="SerializedPropertyType"/> that represents fields of the specified `type`.
        /// </summary>
        public static SerializedPropertyType GetPropertyType(Type type)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Removes the specified array element from the `property`.</summary>
        /// <remarks>
        /// If the element is not at its default value, the first call to
        /// <see cref="SerializedProperty.DeleteArrayElementAtIndex"/> will only reset it, so this method will
        /// call it again if necessary to ensure that it actually gets removed.
        /// </remarks>
        public static void RemoveArrayElement(SerializedProperty property, int index)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Accessor Pool
        /************************************************************************************************************************/

        private static readonly Dictionary<Type, Dictionary<string, PropertyAccessor>>
            TypeToPathToAccessor = new Dictionary<Type, Dictionary<string, PropertyAccessor>>();

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an <see cref="PropertyAccessor"/> that can be used to access the details of the specified `property`.
        /// </summary>
        public static PropertyAccessor GetAccessor(this SerializedProperty property)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns an <see cref="PropertyAccessor"/> for a <see cref="SerializedProperty"/> with the specified `propertyPath`
        /// on the specified `type` of object.
        /// </summary>
        private static PropertyAccessor GetAccessor(SerializedProperty property, string propertyPath, ref Type type)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a field with the specified `name` in the `declaringType` or any of its base types.</summary>
        /// <remarks>Uses the <see cref="InstanceBindings"/>.</remarks>
        public static FieldInfo GetField(PropertyAccessor accessor, SerializedProperty property, Type declaringType, string name)
        {
            return default;
        }

        /// <summary>Returns a field with the specified `name` in the `declaringType` or any of its base types.</summary>
        /// <remarks>Uses the <see cref="InstanceBindings"/>.</remarks>
        public static FieldInfo GetField(Type declaringType, string name)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region PropertyAccessor
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// A wrapper for accessing the underlying values and fields of a <see cref="SerializedProperty"/>.
        /// </summary>
        public class PropertyAccessor
        {
            /************************************************************************************************************************/

            /// <summary>The accessor for the field which this accessor is nested inside.</summary>
            public readonly PropertyAccessor Parent;

            /// <summary>The name of the field wrapped by this accessor.</summary>
            public readonly string Name;

            /// <summary>The field wrapped by this accessor.</summary>
            protected readonly FieldInfo Field;

            /// <summary>
            /// The type of the wrapped <see cref="Field"/>.
            /// Or if it's a collection, this is the type of items in the collection.
            /// </summary>
            protected readonly Type FieldElementType;

            /************************************************************************************************************************/

            /// <summary>[Internal] Creates a new <see cref="PropertyAccessor"/>.</summary>
            internal PropertyAccessor(PropertyAccessor parent, string name, FieldInfo field)
                : this(parent, name, field, field?.FieldType)
            {
            }

            /// <summary>Creates a new <see cref="PropertyAccessor"/>.</summary>
            protected PropertyAccessor(PropertyAccessor parent, string name, FieldInfo field, Type fieldElementType)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Returns the <see cref="Field"/> if there is one or tries to get it from the object's type.</summary>
            /// 
            /// <remarks>
            /// If this accessor has a <see cref="Parent"/>, the `obj` must be associated with the root
            /// <see cref="SerializedProperty"/> and this method will change it to reference the parent field's value.
            /// </remarks>
            /// 
            /// <example><code>
            /// [Serializable]
            /// public class InnerClass
            /// {
            ///     public float value;
            /// }
            /// 
            /// [Serializable]
            /// public class RootClass
            /// {
            ///     public InnerClass inner;
            /// }
            /// 
            /// public class MyBehaviour : MonoBehaviour
            /// {
            ///     public RootClass root;
            /// }
            /// 
            /// [UnityEditor.CustomEditor(typeof(MyBehaviour))]
            /// public class MyEditor : UnityEditor.Editor
            /// {
            ///     private void OnEnable()
            ///     {
            ///         var serializedObject = new SerializedObject(target);
            ///         var rootProperty = serializedObject.FindProperty("root");
            ///         var innerProperty = rootProperty.FindPropertyRelative("inner");
            ///         var valueProperty = innerProperty.FindPropertyRelative("value");
            /// 
            ///         var accessor = valueProperty.GetAccessor();
            /// 
            ///         object obj = target;
            ///         var valueField = accessor.GetField(ref obj);
            ///         // valueField is a FieldInfo referring to InnerClass.value.
            ///         // obj now holds the ((MyBehaviour)target).root.inner.
            ///     }
            /// }
            /// </code></example>
            /// 
            public FieldInfo GetField(ref object obj)
            {
                return default;
            }

            /// <summary>
            /// Returns the <see cref="Field"/> if there is one, otherwise calls <see cref="GetField(ref object)"/>.
            /// </summary>
            public FieldInfo GetField(object obj)
                => Field ?? GetField(ref obj);

            /// <summary>
            /// Calls <see cref="GetField(object)"/> with the <see cref="SerializedObject.targetObject"/>.
            /// </summary>
            public FieldInfo GetField(SerializedObject serializedObject)
                => serializedObject != null ? GetField(serializedObject.targetObject) : null;

            /// <summary>
            /// Calls <see cref="GetField(SerializedObject)"/> with the
            /// <see cref="SerializedProperty.serializedObject"/>.
            /// </summary>
            public FieldInfo GetField(SerializedProperty serializedProperty)
                => serializedProperty != null ? GetField(serializedProperty.serializedObject) : null;

            /************************************************************************************************************************/

            /// <summary>
            /// Returns the <see cref="FieldElementType"/> if there is one, otherwise calls <see cref="GetField(ref object)"/>
            /// and returns its <see cref="FieldInfo.FieldType"/>.
            /// </summary>
            public virtual Type GetFieldElementType(object obj)
                => FieldElementType ?? GetField(ref obj)?.FieldType;

            /// <summary>
            /// Calls <see cref="GetFieldElementType(object)"/> with the
            /// <see cref="SerializedObject.targetObject"/>.
            /// </summary>
            public Type GetFieldElementType(SerializedObject serializedObject)
                => serializedObject != null ? GetFieldElementType(serializedObject.targetObject) : null;

            /// <summary>
            /// Calls <see cref="GetFieldElementType(SerializedObject)"/> with the
            /// <see cref="SerializedProperty.serializedObject"/>.
            /// </summary>
            public Type GetFieldElementType(SerializedProperty serializedProperty)
                => serializedProperty != null ? GetFieldElementType(serializedProperty.serializedObject) : null;

            /************************************************************************************************************************/

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to get and return
            /// the value of the <see cref="Field"/>.
            /// </summary>
            public virtual object GetValue(object obj)
            {
                return default;
            }

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to get and return
            /// the value of the <see cref="Field"/>.
            /// </summary>
            public object GetValue(SerializedObject serializedObject)
                => serializedObject != null ? GetValue(serializedObject.targetObject) : null;

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to get and return
            /// the value of the <see cref="Field"/>.
            /// </summary>
            public object GetValue(SerializedProperty serializedProperty)
                => serializedProperty != null ? GetValue(serializedProperty.serializedObject.targetObject) : null;

            /************************************************************************************************************************/

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to set the value
            /// of the <see cref="Field"/>.
            /// </summary>
            public virtual void SetValue(object obj, object value)
            {
            }

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to set the value
            /// of the <see cref="Field"/>.
            /// </summary>
            public void SetValue(SerializedObject serializedObject, object value)
            {
            }

            /// <summary>
            /// Gets the value of the from the <see cref="Parent"/> (if there is one), then uses it to set the value
            /// of the <see cref="Field"/>.
            /// </summary>
            public void SetValue(SerializedProperty serializedProperty, object value)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Resets the value of the <see cref="SerializedProperty"/> to the default value of its type by executing
            /// its constructor and field initializers.
            /// </summary>
            /// <remarks>
            /// If you don't want to run constructors and field initializers, you can call
            /// <see cref="Serialization.ResetValue"/> instead.
            /// </remarks>
            /// <example><code>
            /// SerializedProperty property;
            /// property.GetAccessor().ResetValue(property);
            /// </code></example>
            public void ResetValue(SerializedProperty property, string undoName = "Inspector")
            {
            }

            /************************************************************************************************************************/

            /// <summary>Returns a description of this accessor's path.</summary>
            public override string ToString()
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns a this accessor's <see cref="SerializedProperty.propertyPath"/>.</summary>
            public virtual string GetPath()
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region CollectionPropertyAccessor
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] A <see cref="PropertyAccessor"/> for a specific element index in a collection.</summary>
        public class CollectionPropertyAccessor : PropertyAccessor
        {
            /************************************************************************************************************************/

            /// <summary>The index of the array element this accessor targets.</summary>
            public readonly int ElementIndex;

            /************************************************************************************************************************/

            /// <summary>[Internal] Creates a new <see cref="CollectionPropertyAccessor"/>.</summary>
            internal CollectionPropertyAccessor(PropertyAccessor parent, string name, FieldInfo field, int elementIndex)
                : base(parent, name, field, GetElementType(field?.FieldType))
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override Type GetFieldElementType(object obj) => FieldElementType ?? GetElementType(GetField(ref obj)?.FieldType);

            /************************************************************************************************************************/

            /// <summary>Returns the type of elements in the array.</summary>
            public static Type GetElementType(Type fieldType)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns the collection object targeted by this accessor.</summary>
            public object GetCollection(object obj) => base.GetValue(obj);

            /// <inheritdoc/>
            public override object GetValue(object obj)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Sets the collection object targeted by this accessor.</summary>
            public void SetCollection(object obj, object value) => base.SetValue(obj, value);

            /// <inheritdoc/>
            public override void SetValue(object obj, object value)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Returns a description of this accessor's path.</summary>
            public override string ToString() => $"{base.ToString()}[{ElementIndex}]";

            /************************************************************************************************************************/

            /// <summary>Returns the <see cref="SerializedProperty.propertyPath"/> of the array containing the target.</summary>
            public string GetCollectionPath() => base.GetPath();

            /// <summary>Returns this accessor's <see cref="SerializedProperty.propertyPath"/>.</summary>
            public override string GetPath() => $"{base.GetPath()}{ArrayDataPrefix}{ElementIndex}{ArrayDataSuffix}";

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
