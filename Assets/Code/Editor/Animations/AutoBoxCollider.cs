using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Animations {
    public static class AutoBoxCollider {
        const int TargetLayer = 24; // Target layer
        const int SourceLayer = 22; // Source layer from which bones will be selected

        [MenuItem("GameObject/Add Hitbox Colliders", false, 10)]
        public static void AddCollidersToSelectedBones(MenuCommand menuCommand) {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject != null) {
                AddCollidersToBones(selectedObject);
            } else {
                Debug.LogWarning("No GameObject selected. Please select a GameObject in the hierarchy.");
            }
        }

        static void AddCollidersToBones(GameObject rootObject) {
            // Get all Transforms (bones) in the object and its children
            Transform[] bones = rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform bone in bones) {
                // Check if the bone is on layer 22
                if (bone.gameObject.layer == SourceLayer) {
                    Collider existingCollider = bone.GetComponent<Collider>();

                    if (bone != rootObject.transform && existingCollider != null) {
                        // Create a new GameObject for the HitBox
                        GameObject hitBoxObject = new GameObject(bone.name + "_HitBox");
                        hitBoxObject.transform.SetParent(bone); // Set parent to the current bone
                        hitBoxObject.transform.localPosition = Vector3.zero; // Reset local position
                        hitBoxObject.transform.localRotation = Quaternion.identity; // Reset local rotation
                        hitBoxObject.transform.localScale = Vector3.one; // Reset scale

                        // Set the layer for the new GameObject
                        hitBoxObject.layer = TargetLayer;

                        // Add BoxCollider to the new object
                        BoxCollider boxCollider = hitBoxObject.AddComponent<BoxCollider>();

                        // Transfer the center and size values from the existing Collider to the new BoxCollider
                        if (existingCollider is BoxCollider existingBoxCollider) {
                            boxCollider.center = existingBoxCollider.center;
                            boxCollider.size = existingBoxCollider.size;
                        } else if (existingCollider is SphereCollider existingSphereCollider) {
                            boxCollider.center = existingSphereCollider.center;
                            boxCollider.size = Vector3.one * existingSphereCollider.radius * 2; // Convert radius to size
                        } else if (existingCollider is CapsuleCollider existingCapsuleCollider) {
                            boxCollider.center = existingCapsuleCollider.center;
                            boxCollider.size = new Vector3(
                                existingCapsuleCollider.radius * 2,
                                existingCapsuleCollider.height,
                                existingCapsuleCollider.radius * 2);
                        } else if (existingCollider is MeshCollider existingMeshCollider) {
                            boxCollider.center = existingMeshCollider.bounds.center - bone.position;
                            boxCollider.size = existingMeshCollider.bounds.size;
                        } else {
                            Debug.LogWarning($"Collider on {bone.name} is not a supported type. Center and Size will not be copied correctly.");
                        }

                        Debug.Log($"HitBox Collider added to {bone.name} with center: {boxCollider.center} and size: {boxCollider.size}");
                    }
                }
            }

            Debug.Log("HitBox Colliders added to bones of " + rootObject.name);
        }
    }
}