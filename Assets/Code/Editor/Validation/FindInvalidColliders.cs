using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Validation {
    public static class FindInvalidColliders {
        [MenuItem("TG/Assets/Find Invalid Colliders", priority = -100)]
        static void Find() {
            HashSet<Object> objects = new();
            foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)) {
                // BoxCollider is the only one user can set negative dimensions
                if (collider is BoxCollider boxCollider && IsNegative(boxCollider.size)) {
                    AddInvalidObject(objects, collider);
                    continue;
                }

                var transform = collider.transform;
                while (transform != null) {
                    if (IsNegative(transform.localScale)) {
                        AddInvalidObject(objects, transform);
                    }
                    transform = transform.parent;
                }
            }
        }

        static bool IsNegative(Vector3 vector) {
            return vector.x < 0 || vector.y < 0 || vector.z < 0;
        }

        static void AddInvalidObject(HashSet<Object> objects, Object context) {
            if (objects.Add(context)) {
                Log.Important?.Error("Negative Scale or Size", context);
            }
        }
    }
}