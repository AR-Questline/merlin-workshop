using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor {
    [CustomPropertyDrawer(typeof(SerializableGuid))]
    public class SerializableGuidDrawer : PropertyDrawer {
        public override unsafe void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var part1Property = property.FindPropertyRelative("_guidPart1");
            var part2Property = property.FindPropertyRelative("_guidPart2");
            var part3Property = property.FindPropertyRelative("_guidPart3");
            var part4Property = property.FindPropertyRelative("_guidPart4");

            Span<int> guidSpan = stackalloc int[4];
            guidSpan[0] = part1Property.intValue;
            guidSpan[1] = part2Property.intValue;
            guidSpan[2] = part3Property.intValue;
            guidSpan[3] = part4Property.intValue;

            var systemGuid = *(Guid*)UnsafeUtility.AddressOf(ref guidSpan.GetPinnableReference());
            var stringGuid = systemGuid.ToString("N");

            EditorGUI.BeginChangeCheck();
            stringGuid = EditorGUI.DelayedTextField(position, label, stringGuid);
            if (!EditorGUI.EndChangeCheck()) {
                return;
            }

            if (Guid.TryParse(stringGuid, out var newGuid)) {
                part1Property.intValue = UnsafeUtility.ReadArrayElement<int>(&newGuid, 0);
                part2Property.intValue = UnsafeUtility.ReadArrayElement<int>(&newGuid, 1);
                part3Property.intValue = UnsafeUtility.ReadArrayElement<int>(&newGuid, 2);
                part4Property.intValue = UnsafeUtility.ReadArrayElement<int>(&newGuid, 3);
            } else {
                Debug.LogWarning("Invalid GUID format entered.");
            }
        }
    }
}