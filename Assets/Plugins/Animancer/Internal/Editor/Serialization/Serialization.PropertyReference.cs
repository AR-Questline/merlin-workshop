// Serialization // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Shared File Last Modified: 2021-12-11.
namespace Animancer.Editor
// namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] Various serialization utilities.</summary>
    public partial class Serialization
    {
        /// <summary>[Editor-Only] A serializable reference to a <see cref="SerializedProperty"/>.</summary>
        [Serializable]
        public class PropertyReference
        {
            /************************************************************************************************************************/

            [SerializeField] private ObjectReference[] _TargetObjects;

            /// <summary>[<see cref="SerializeField"/>] The <see cref="SerializedObject.targetObject"/>.</summary>
            public ObjectReference TargetObject
            {
                get
                {
                    return _TargetObjects != null && _TargetObjects.Length > 0 ?
                        _TargetObjects[0] : null;
                }
            }

            /// <summary>[<see cref="SerializeField"/>] The <see cref="SerializedObject.targetObjects"/>.</summary>
            public ObjectReference[] TargetObjects => _TargetObjects;

            /************************************************************************************************************************/

            [SerializeField] private ObjectReference _Context;

            /// <summary>[<see cref="SerializeField"/>] The <see cref="SerializedObject.context"/>.</summary>
            public ObjectReference Context => _Context;

            /************************************************************************************************************************/

            [SerializeField] private string _PropertyPath;

            /// <summary>[<see cref="SerializeField"/>] The <see cref="SerializedProperty.propertyPath"/>.</summary>
            public string PropertyPath => _PropertyPath;

            /************************************************************************************************************************/

            [NonSerialized] private bool _IsInitialized;

            /// <summary>Indicates whether the <see cref="Property"/> has been accessed.</summary>
            public bool IsInitialized => _IsInitialized;

            /************************************************************************************************************************/

            [NonSerialized] private SerializedProperty _Property;

            /// <summary>[<see cref="SerializeField"/>] The referenced <see cref="SerializedProperty"/>.</summary>
            public SerializedProperty Property
            {
                get
                {
                    Initialize();
                    return _Property;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="PropertyReference"/> which wraps the specified `property`.
            /// </summary>
            public PropertyReference(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="PropertyReference"/> which wraps the specified `property`.
            /// </summary>
            public static implicit operator PropertyReference(SerializedProperty property) => new PropertyReference(property);

            /// <summary>
            /// Returns the target <see cref="Property"/>.
            /// </summary>
            public static implicit operator SerializedProperty(PropertyReference reference) => reference.Property;

            /************************************************************************************************************************/

            private void Initialize()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Do the specified `property` and `targetObjects` match the targets of this reference?</summary>
            public bool IsTarget(SerializedProperty property, Object[] targetObjects)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Is there is at least one target and none of them are <c>null</c>?</summary>
            private bool TargetsExist
            {
                get
                {
                    if (_TargetObjects == null ||
                        _TargetObjects.Length == 0)
                        return false;

                    for (int i = 0; i < _TargetObjects.Length; i++)
                    {
                        if (_TargetObjects[i].Object == null)
                            return false;
                    }

                    return true;
                }
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="SerializedObject.Update"/> if the <see cref="Property"/> has been initialized.
            /// </summary>
            public void Update()
            {
            }

            /// <summary>
            /// Calls <see cref="SerializedObject.ApplyModifiedProperties"/> if the <see cref="Property"/> has been initialized.
            /// </summary>
            public void ApplyModifiedProperties()
            {
            }

            /// <summary>
            /// Calls <see cref="SerializedObject.Dispose"/> if the <see cref="Property"/> has been initialized.
            /// </summary>
            public void Dispose()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Gets the height needed to draw the target property.</summary>
            public float GetPropertyHeight()
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Draws the target object within the specified `area`.</summary>
            public void DoTargetGUI(Rect area)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws the target property within the specified `area`.</summary>
            public void DoPropertyGUI(Rect area)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the `reference` and <see cref="PropertyReference.Property"/> are not null.</summary>
        public static bool IsValid(this PropertyReference reference) => reference?.Property != null;

        /************************************************************************************************************************/
    }
}

#endif
