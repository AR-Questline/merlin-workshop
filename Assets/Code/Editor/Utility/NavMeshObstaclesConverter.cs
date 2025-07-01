using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public static class NavMeshObstaclesConverter {
    //[MenuItem("TG/NavMeshObstacles/ConvertNavMeshObstacles")]
        public static void ConvertNavMeshObstacles() {
            var locationPrefabsGUIDs = AssetDatabase.FindAssets("t:Prefab");
            List<GameObject> locationPrefabs = new List<GameObject>();
            foreach (var guid in locationPrefabsGUIDs) {
                var x = AssetsUtils.LoadAssetByGuid<GameObject>(guid);
                if (x.GetComponentInChildren<LocationPrefab>() != null) {
                    locationPrefabs.Add(x);
                }
            }

            foreach (var location in locationPrefabs) {
                bool obstacleFound = false;
                var obstacles = location.GetComponentsInChildren<GridGraphObstacle>();
                foreach (var obstacle in obstacles) {
                    obstacleFound = true;
                    if (obstacle.GetComponent<Collider>() == null) {
                        Object.DestroyImmediate(obstacle.gameObject, true);      
                        continue;
                    }
                    GameObject go = obstacle.gameObject;
                    Vector3 position = go.transform.position;
                    Vector3 colliderOffset;
                    try {
                        switch (obstacle.colliderType) {
                            case ArColliderType.Box:
                                var boxCollider = obstacle.GetComponent<BoxCollider>();
                                colliderOffset = boxCollider.center;
                                position.x += colliderOffset.x;
                                position.y = -25f;
                                position.z += colliderOffset.z;
                                Vector3 colliderSize = boxCollider.size;
                                colliderSize.x *= go.transform.localScale.x;
                                colliderSize.y *= go.transform.localScale.y;
                                colliderSize.z *= go.transform.localScale.z;
                                // Set correct values
                                go.transform.position = position;
                                go.transform.localScale = Vector3.one;
                                boxCollider.center = Vector3.zero;
                                boxCollider.size = colliderSize;
                                break;
                            case ArColliderType.Capsule:
                                var capsuleCollider = obstacle.GetComponent<CapsuleCollider>();
                                colliderOffset = capsuleCollider.center;
                                position.x += colliderOffset.x;
                                position.y = -25f;
                                position.z += colliderOffset.z;
                                float radius = capsuleCollider.radius * go.transform.localScale.z;
                                // Set correct values
                                go.transform.position = position;
                                go.transform.localScale = Vector3.one;
                                capsuleCollider.center = Vector3.zero;
                                capsuleCollider.radius = radius;
                                capsuleCollider.height = 50f;
                                break;
                            case ArColliderType.Sphere:
                                var sphereCollider = obstacle.GetComponent<SphereCollider>();
                                colliderOffset = sphereCollider.center;
                                position.x += colliderOffset.x;
                                position.y = -25f;
                                position.z += colliderOffset.z;
                                float r = sphereCollider.radius * go.transform.localScale.z;
                                // Set correct values
                                go.transform.position = position;
                                go.transform.localScale = Vector3.one;
                                sphereCollider.center = Vector3.zero;
                                sphereCollider.radius = r;
                                break;
                        }
                    } catch (Exception e) {
                        Log.Important?.Error($"Execption happend in location {go.transform.parent.gameObject.name}");
                        Log.Important?.Error(e.ToString());
                    }
                }
                if (obstacleFound) {
                    EditorUtility.SetDirty(location);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
