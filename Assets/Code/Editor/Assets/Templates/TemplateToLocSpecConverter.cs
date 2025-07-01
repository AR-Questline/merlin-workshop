using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.Utility.GameObjects;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Assets.Templates {
    [InitializeOnLoad]
    public static class TemplateToLocSpecConverter {
        static TemplateToLocSpecConverter() {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        static void OnHierarchyChanged() {
            foreach (GameObject gameObject in Selection.gameObjects) {
                if (gameObject != null && IsValidScene(Selection.activeGameObject.scene, Selection.activeGameObject)) {
                    if (gameObject.TryGetComponent(out ItemTemplate itemTemplate)) {
                        if (CheckPrefabChange(gameObject.scene, gameObject)) {
                            ConvertToPickableSpec(itemTemplate);
                        }
                    } else if (gameObject.TryGetComponent(out LocationTemplate locationTemplate)) {
                        if (UniqueNpcUtils.IsUnique(locationTemplate) && CheckPrefabChange(gameObject.scene, gameObject)) {
                            ConvertToNpcPresence(locationTemplate);
                        } else if (gameObject.HasComponent<MountAttachment>() && CheckPrefabChange(gameObject.scene, gameObject)) {
                            ConvertToMountSpawner(locationTemplate);
                        }
                    }
                }
            }
        }

        static bool IsValidScene(Scene scene, GameObject activeObject) {
            return scene.IsValid() &&
                   (StageUtility.GetStage(scene) is not PrefabStage prefabStage || prefabStage.prefabContentsRoot != activeObject);
        }

        static bool CheckPrefabChange(Scene scene, GameObject activeObject) {
            return StageUtility.GetStage(scene) is not PrefabStage ||
                   EditorUtility.DisplayDialog("Convert to LocSpec?",
                       $"Do you want to convert {activeObject} into location spec?"
                       , "Yep", "Nah");
        }

        static void ConvertToPickableSpec(ItemTemplate template) {
            CreatePickableSpec(template);
            Object.DestroyImmediate(template.gameObject);
        }

        static void CreatePickableSpec(ItemTemplate template) {
            var go = new GameObject(template.name);
            StageUtility.PlaceGameObjectInCurrentStage(go);

            go.isStatic = false;
            go.transform.SetParent(template.transform.parent);
            go.transform.position = template.transform.position;
            
            var spec = go.AddComponent<PickableSpec>();
            spec.Setup(GetTemplateReference(template));
        }

        static void ConvertToNpcPresence(LocationTemplate template) {
            CreateNpcPresence(template);
            Object.DestroyImmediate(template.gameObject);
        }

        static void CreateNpcPresence(LocationTemplate template) {
            var go = new GameObject($"NPCPresence_{template.name.TrimStart("Spec_NPC_")}");
            var transform = go.transform;
            StageUtility.PlaceGameObjectInCurrentStage(go);
            
            go.isStatic = false;
            transform.SetParent(template.transform.parent);
            transform.position = template.transform.position;
            
            var locationSpec = go.AddComponent<LocationSpec>();
            locationSpec.snapToGround = true;
            Ground.SnapToGroundSafe(transform);
            
            var npcPresenceAttachment = locationSpec.AddComponent<NpcPresenceAttachment>();
            npcPresenceAttachment.SetLocation(GetTemplateReference(template));

            locationSpec.AddComponent<IdleDataAttachment>();
            
            locationSpec.ValidatePrefab(true);
        }
        
        static void ConvertToMountSpawner(LocationTemplate template) {
            CreateMountSpawner(template);
            Object.DestroyImmediate(template.gameObject);
        }
        
        static void CreateMountSpawner(LocationTemplate template) {
            var go = new GameObject($"Spec_MountSpawner_{template.name.TrimStart("Spec_Mount_")}");
            var transform = go.transform;
            StageUtility.PlaceGameObjectInCurrentStage(go);
            
            go.isStatic = false;
            transform.SetParent(template.transform.parent);
            transform.position = template.transform.position;
            
            var locationSpec = go.AddComponent<LocationSpec>();
            locationSpec.snapToGround = true;
            Ground.SnapToGroundSafe(transform);
            
            var mountSpawner = locationSpec.AddComponent<MountSpawnerAttachment>();
            mountSpawner.SetLocation(GetTemplateReference(template));
            
            locationSpec.ValidatePrefab(true);
        }

        static TemplateReference GetTemplateReference(Template template) {
            Template prefab = PrefabUtility.GetCorrespondingObjectFromSource(template);
            TemplatesUtil.EDITOR_AssignGuid(template, prefab);
            return new TemplateReference(template);
        }
    }
}
