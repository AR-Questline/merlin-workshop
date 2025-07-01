// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Animancer.Editor
{
    /// <summary>
    /// An object that holds a serialized callback (a <see cref="UnityEvent"/> by default) so that empty ones can be
    /// drawn in the GUI without allocating array space for them until they actually contain something.
    /// </summary>
    internal class DummySerializableCallback : ScriptableObject
    {
        /************************************************************************************************************************/

        [SerializeField] private SerializableCallbackHolder _Holder;

        /************************************************************************************************************************/

        private static SerializedProperty _CallbackProperty;

        private static SerializedProperty CallbackProperty
        {
            get
            {
                if (_CallbackProperty == null)
                {
                    var instance = CreateInstance<DummySerializableCallback>();

                    instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                    var serializedObject = new SerializedObject(instance);
                    _CallbackProperty = serializedObject.FindProperty(
                        $"{nameof(_Holder)}.{SerializableCallbackHolder.CallbackField}");

                    AssemblyReloadEvents.beforeAssemblyReload += () =>
                    {
                        serializedObject.Dispose();
                        DestroyImmediate(instance);
                    };
                }

                return _CallbackProperty;
            }
        }

        /************************************************************************************************************************/

        public static float Height => EditorGUI.GetPropertyHeight(CallbackProperty);

        /************************************************************************************************************************/

        public static bool DoCallbackGUI(ref Rect area, GUIContent label, SerializedProperty property,
            out object callback)
        {
            callback = default(object);
            return default;
        }

        /************************************************************************************************************************/
    }
}

#endif
