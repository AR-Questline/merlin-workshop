using System.Linq;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Utility;
using AwesomeTechnologies;
using UnityEditor;

using UnityEngine;

namespace Awaken.TG.Editor.Main.Locations
{
    [CustomEditor(typeof(LocationPrefab))]
    public class LocationPrefabEditor : UnityEditor.Editor {
        // === Items

        public static readonly ChecklistItem<SelectionIndicator> SelectionIndicator = new ChecklistItem<SelectionIndicator>() {
            name = "selection indicator",
            missingText = "No selection indicator, please add one, position and scale it properly.",
            findObjectFn = ChecklistItemFunctions.FindChildWith<SelectionIndicator>(),
            createObjectFn = ChecklistItemFunctions.CreateByAddingPrefab<SelectionIndicator>("Assets/Resources/Prefabs/Utility/SelectionIndicator.prefab")
        };

        public static readonly ChecklistItem<Collider> Collider = new ChecklistItem<Collider>() {
            name = "collider",
            missingText = "No collider, please add one, position and scale it properly so that the player can click on the location.",
            findObjectFn = (root) => {
                Collider[] colliders = root.GetComponentsInChildren<Collider>();
                return colliders.FirstOrDefault(c => c.GetComponent<MeshRenderer>() == null);
            },
            createObjectFn = ChecklistItemFunctions.CreateObjectWithBehaviour<CapsuleCollider>("LocationCollider")
        };

        public static readonly ChecklistItem<Transform> CharacterPlacement = new ChecklistItem<Transform>() {
            name = "character placement",
            missingText = "No character placement configured, please add a place for the characters to stand in.",
            findObjectFn = ChecklistItemFunctions.FindChildWithName<Transform>("CharacterPlacement"),
            createObjectFn = ChecklistItemFunctions.CreateByAddingPrefab<Transform>("Assets/Resources/Prefabs/Utility/CharacterPlacement.prefab")
        };

        // public static readonly ChecklistItem<VegetationMaskArea> VegetationMask = new ChecklistItem<VegetationMaskArea>() {
        //     name = "vegetation mask",
        //     missingText = "No vegetation mask, please add and setup one.",
        //     findObjectFn = (root) => {
        //         return root.GetComponentInChildren<VegetationMaskArea>();
        //     },
        //     createObjectFn = root => {
        //         var mask = ChecklistItemFunctions.CreateObjectWithBehaviour<VegetationMaskArea>("VegetationMaskArea")(root);
        //         Bounds bounds = TransformBoundsUtil.FindBounds(root.transform, false, "SelectionIndicator");
        //         bounds.Expand(2);
        //         mask.ClearNodes();
        //         mask.AddNode(new Vector3(bounds.min.x, 0, bounds.min.z));
        //         mask.AddNode(new Vector3(bounds.max.x, 0, bounds.max.z));
        //         mask.AddNode(new Vector3(bounds.max.x, 0, bounds.min.z));
        //         mask.AddNode(new Vector3(bounds.min.x, 0, bounds.max.z));
        //         return mask;
        //     }
        // };
        
        public static readonly ChecklistItem<GridGraphObstacle> NavmeshObstacle = new ChecklistItem<GridGraphObstacle>() {
            name = "Navmesh Obstacle",
            missingText = $"No grid graph obstacle, please add {nameof(GridGraphObstacle)} and collider for no walkable area of location",
            findObjectFn = (root) => root.GetComponentInChildren<GridGraphObstacle>(),
            createObjectFn = root => {
                
                GameObject obstacleGo = new GameObject("NavmeshObstacle");
                obstacleGo.transform.parent = root.transform;
                obstacleGo.transform.localPosition = Vector3.zero;
                
                var locationCollider = root.GetComponentsInChildren<Collider>().FirstOrDefault(c => c.GetComponent<MeshRenderer>() == null);
                if (locationCollider == null) {
                    // Create new collider for GridGraphObstacle
                    Bounds bounds = TransformBoundsUtil.FindBounds(root.transform, false, "SelectionIndicator");

                    var boxCollider = obstacleGo.gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = bounds.size * 0.8f;
                    boxCollider.center = bounds.center;
                    var obstacle = obstacleGo.AddComponent<GridGraphObstacle>();
                    obstacle.colliderType = ArColliderType.Box;
                } else {
                    // Use existing collider but shrank version
                    if (locationCollider is BoxCollider boxCollider) {
                        var obstacleCollider = obstacleGo.gameObject.AddComponent<BoxCollider>();
                        obstacleCollider.size = boxCollider.size * 0.8f;
                        obstacleCollider.center = boxCollider.center;
                        
                        var obstacle = obstacleGo.AddComponent<GridGraphObstacle>();
                        obstacle.colliderType = ArColliderType.Box;
                    }else if (locationCollider is SphereCollider sphereCollider) {
                        var obstacleCollider = obstacleGo.gameObject.AddComponent<SphereCollider>();
                        obstacleCollider.radius = sphereCollider.radius * 0.8f;
                        obstacleCollider.center = sphereCollider.center;
                        
                        var obstacle = obstacleGo.AddComponent<GridGraphObstacle>();
                        obstacle.colliderType = ArColliderType.Sphere;
                    }else if (locationCollider is CapsuleCollider capsuleCollider) {
                        var obstacleCollider = obstacleGo.gameObject.AddComponent<CapsuleCollider>();
                        obstacleCollider.radius = capsuleCollider.radius * 0.8f;
                        obstacleCollider.height = capsuleCollider.height * 0.8f;
                        obstacleCollider.center = capsuleCollider.center;
                        
                        var obstacle = obstacleGo.AddComponent<GridGraphObstacle>();
                        obstacle.colliderType = ArColliderType.Capsule;
                    }
                }
                return obstacleGo.GetComponent<GridGraphObstacle>();
            }
        };

        // === GUI code

        public override void OnInspectorGUI() {
            if (Application.isPlaying) return;

            Component root = (Component)target;

            // show button to enter prefab mode
            if ((UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot != root.gameObject) && GUILayout.Button("Go inside prefab"))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject)));
            }

            GUILayout.Label("Location prefab checklist:");
            Collider.DisplayGUI(root);
            NavmeshObstacle.DisplayGUI(root);
            SelectionIndicator.DisplayGUI(root);
            CharacterPlacement.DisplayGUI(root);
            //VegetationMask.DisplayGUI(root);
        }
    }
}
