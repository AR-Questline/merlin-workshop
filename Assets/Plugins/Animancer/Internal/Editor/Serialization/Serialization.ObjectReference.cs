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
        /// <summary>[Editor-Only]
        /// Directly serializing an <see cref="UnityEngine.Object"/> reference doesn't always work (such as with scene
        /// objects when entering Play Mode), so this class also serializes their instance ID and uses that if the direct
        /// reference fails.
        /// </summary>
        [Serializable]
        public class ObjectReference
        {
            /************************************************************************************************************************/

            [SerializeField] private Object _Object;
            [SerializeField] private int _InstanceID;

            /************************************************************************************************************************/

            /// <summary>The referenced <see cref="SerializedObject"/>.</summary>
            public Object Object
            {
                get
                {
                    Initialize();
                    return _Object;
                }
            }

            /// <summary>The <see cref="Object.GetInstanceID"/>.</summary>
            public int InstanceID => _InstanceID;

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ObjectReference"/> which wraps the specified
            /// <see cref="UnityEngine.Object"/>.
            /// </summary>
            public ObjectReference(Object obj)
            {
            }

            /************************************************************************************************************************/

            private void Initialize()
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="ObjectReference"/> which wraps the specified
            /// <see cref="UnityEngine.Object"/>.
            /// </summary>
            public static implicit operator ObjectReference(Object obj) => new ObjectReference(obj);

            /// <summary>Returns the target <see cref="Object"/>.</summary>
            public static implicit operator Object(ObjectReference reference) => reference.Object;

            /************************************************************************************************************************/

            /// <summary>Creates a new array of <see cref="ObjectReference"/>s representing the `objects`.</summary>
            public static ObjectReference[] Convert(params Object[] objects)
            {
                return default;
            }

            /// <summary>
            /// Creates a new array of <see cref="UnityEngine.Object"/>s containing the target <see cref="Object"/> of each
            /// of the `references`.
            /// </summary>
            public static Object[] Convert(params ObjectReference[] references)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Indicates whether both arrays refer to the same set of objects.</summary>
            public static bool AreSameObjects(ObjectReference[] references, Object[] objects)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns a string describing this object.</summary>
            public override string ToString()
                => $"Serialization.ObjectReference [{_InstanceID}] {_Object}";

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the `reference` and <see cref="ObjectReference.Object"/> are not null.</summary>
        public static bool IsValid(this ObjectReference reference)
            => reference?.Object != null;

        /************************************************************************************************************************/
    }
}

#endif
