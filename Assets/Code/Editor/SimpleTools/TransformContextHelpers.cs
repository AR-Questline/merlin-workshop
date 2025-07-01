using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SimpleTools {
    public static class TransformContextHelpers {
        [MenuItem("CONTEXT/Transform/Copy Global Position", false, 0)]
        static void TransformCopyGlobalPosition(MenuCommand command) {
            Transform transform = command.context as Transform;
            if (transform == null) return;

            Vector3 position = transform.position;
            EditorGUIUtility.systemCopyBuffer = position.ToString();
        }
    }
}
