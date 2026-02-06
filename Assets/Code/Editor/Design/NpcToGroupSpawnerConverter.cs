using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Design {
    public static class NpcToGroupSpawnerConverter {
        [MenuItem("GameObject/TG/Convert NPCs to Group Spawner", false, 0)]
        static void ConvertSelectedNpcsToGroupSpawner() {
            var selectedObjects = Selection.gameObjects;
            var npcAttachments = new List<(GameObject gameObject, RepetitiveNpcAttachment attachment)>();
            
            foreach (var obj in selectedObjects) {
                // Only process objects that are in a scene (not prefab assets)
                if (string.IsNullOrEmpty(obj.scene.name)) {
                    continue;
                }
                
                var npcAttachment = obj.GetComponent<RepetitiveNpcAttachment>();
                if (npcAttachment != null) {
                    npcAttachments.Add((obj, npcAttachment));
                }
            }
            
            if (npcAttachments.Count == 0) {
                EditorUtility.DisplayDialog("No Valid NPCs Found", 
                    "No scene GameObjects with RepetitiveNpcAttachment found in selection.", "OK");
                return;
            }
            
            var groupSpawnerObj = CreateGroupSpawnerGameObject(npcAttachments);
            if (groupSpawnerObj == null) {
                return;
            }
            
            foreach (var (gameObject, _) in npcAttachments) {
                Undo.DestroyObjectImmediate(gameObject);
            }
            
            Selection.activeGameObject = groupSpawnerObj;
            EditorGUIUtility.PingObject(groupSpawnerObj);
            
            Log.Important?.Info($"Converted {npcAttachments.Count} NPCs to Group Spawner: {groupSpawnerObj.name}");
        }
        
        [MenuItem("GameObject/TG/Convert NPCs to Group Spawner", true)]
        static bool ValidateConvertSelectedNpcsToGroupSpawner() {
            if (Selection.gameObjects.Length == 0) {
                return false;
            }
            
            // Check if any selected objects are in a scene (not prefab assets)
            bool hasSceneObjects = false;
            bool hasNpcAttachments = false;
            
            foreach (var obj in Selection.gameObjects) {
                // Check if object is in a scene (has a valid scene name)
                if (!string.IsNullOrEmpty(obj.scene.name)) {
                    hasSceneObjects = true;
                    
                    // Check if it has RepetitiveNpcAttachment
                    var npcAttachment = obj.GetComponent<RepetitiveNpcAttachment>();
                    if (npcAttachment != null) {
                        hasNpcAttachments = true;
                    }
                }
            }
            
            return hasSceneObjects && hasNpcAttachments;
        }
        
        static GameObject CreateGroupSpawnerGameObject(List<(GameObject gameObject, RepetitiveNpcAttachment attachment)> npcAttachments) {
            var positions = npcAttachments.Select(item => item.gameObject.transform.position).ToArray();
            var centerPosition = positions.Aggregate(Vector3.zero, (sum, pos) => sum + pos) / positions.Length;
            
            var firstSourceObject = npcAttachments[0].gameObject;
            var groupSpawnerObj = new GameObject("GroupSpawner_ConvertedNPCs");
            groupSpawnerObj.transform.SetParent(firstSourceObject.transform.parent);
            groupSpawnerObj.transform.position = centerPosition;
            
            Undo.RegisterCreatedObjectUndo(groupSpawnerObj, "Convert NPCs to Group Spawner");
            
            Undo.AddComponent<LocationTemplate>(groupSpawnerObj);
            var groupSpawner = Undo.AddComponent<GroupSpawnerAttachment>(groupSpawnerObj);
            
            ConfigureGroupSpawner(groupSpawner, npcAttachments, groupSpawnerObj.transform);
            
            EditorUtility.SetDirty(groupSpawnerObj);
            EditorUtility.SetDirty(groupSpawner);
            
            return groupSpawnerObj;
        }
        
        static void ConfigureGroupSpawner(GroupSpawnerAttachment groupSpawner, 
            List<(GameObject gameObject, RepetitiveNpcAttachment attachment)> npcAttachments, 
            Transform spawnerTransform) {
            
            groupSpawner.discardAfterAllKilled = true;
            
            GroupSpawnerAttachment.EditorAccess.SetToStaticMode(groupSpawner);
            GroupSpawnerAttachment.EditorAccess.ClearLocationsWithPositions(groupSpawner);
            
            foreach (var (gameObject, _) in npcAttachments) {
                var prefabAsset = GetPrefabAsset(gameObject);
                if (prefabAsset == null) {
                    Log.Important?.Warning($"Could not find prefab asset for {gameObject.name}, skipping");
                    continue;
                }
                
                var locationTemplate = prefabAsset.GetComponent<LocationTemplate>();
                if (locationTemplate == null) {
                    Log.Important?.Warning($"Prefab {prefabAsset.name} does not have LocationTemplate component, skipping");
                    continue;
                }
                
                var locationTemplateRef = new TemplateReference(locationTemplate);
                var matrix = CalculateRelativeMatrix(gameObject.transform, spawnerTransform);
                var nextId = GroupSpawnerAttachment.EditorAccess.GetNextId(groupSpawner);
                var locationWithPosition = GroupSpawnerAttachment.EditorAccess.CreateLocationWithPosition(
                    locationTemplateRef, nextId, matrix);
                
                GroupSpawnerAttachment.EditorAccess.AddLocationWithPosition(groupSpawner, locationWithPosition);
                
                Log.Important?.Info($"Added location {prefabAsset.name} with ID {nextId}");
            }
        }
        
        static GameObject GetPrefabAsset(GameObject gameObject) {
            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefabAsset == null) {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                if (!string.IsNullOrEmpty(prefabPath)) {
                    prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
            }
            return prefabAsset;
        }
        
        static Matrix4x4 CalculateRelativeMatrix(Transform sourceTransform, Transform spawnerTransform) {
            var relativePos = spawnerTransform.InverseTransformPoint(sourceTransform.position);
            var relativeRot = Quaternion.Inverse(spawnerTransform.rotation) * sourceTransform.rotation;
            var relativeScale = sourceTransform.localScale;
            return Matrix4x4.TRS(relativePos, relativeRot, relativeScale);
        }
    }
}
