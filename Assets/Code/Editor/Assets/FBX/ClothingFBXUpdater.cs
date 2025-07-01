using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using MagicaCloth2;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using MoreLinq = Awaken.Utility.Collections.MoreLinq;

namespace Awaken.TG.Editor.Assets.FBX {
    [Serializable]
    struct ClothingToUpdate {
        const string FBXExtension = ".fbx";
        const string ToReplaceSuffix = "_FORREPLACEMENT";
        
        public bool CanBeUpdated => Selection != null && ReplacementMesh != null;
        [ShowInInspector, ReadOnly] public GameObject Selection { get; private set; }
        [ShowInInspector, ReadOnly] public GameObject ReplacementMesh { get; private set; }
        
        public string OldMeshPath { get; private set; }
        public string NewMeshPath { get; private set; }

        public ClothingToUpdate(GameObject selection) : this() {
            this.Selection = selection;
            Refresh();
        }

        public void Refresh() {
            ReplacementMesh = null;
            
            if (Selection == null) return;
                
            var oldMesh = Selection.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh;
            if (oldMesh == null) return;
                
            OldMeshPath = AssetDatabase.GetAssetPath(oldMesh);
            if (string.IsNullOrEmpty(OldMeshPath)) return;
                
            NewMeshPath = OldMeshPath.Replace(FBXExtension, ToReplaceSuffix + FBXExtension);
            if (!File.Exists(NewMeshPath)) return;
                
            ReplacementMesh = AssetDatabase.LoadAssetAtPath<GameObject>(NewMeshPath);
        }
    }
    
    public class ClothingFBXUpdater : OdinEditorWindow {
        const int Width = 500;
        const int Height = 300;

        static ClothingFBXUpdater s_editor;
        static int s_x, s_y;

        [ShowInInspector, ListDrawerSettings(HideAddButton = true)] List<ClothingToUpdate> _selection = new(4);
        
        bool HasMeshForReplacement() => _selection.Any(c => c.CanBeUpdated);

        [MenuItem("TG/Assets/Clothing FBX Updater", priority = 100)]
        static void ShowEditor() {
            s_editor = EditorWindow.GetWindow<ClothingFBXUpdater>();
            CenterWindow();
        }
        
        static void CenterWindow() {
            s_editor = EditorWindow.GetWindow<ClothingFBXUpdater>();
            s_x = (Screen.currentResolution.width - Width) / 2;
            s_y = (Screen.currentResolution.height - Height) / 2;
            s_editor.position = new Rect(s_x, s_y, Width, Height);
            s_editor.titleContent = new GUIContent("Clothing FBX Updater");
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            var selectedObjects = Selection.gameObjects;
            if (MoreLinq.IsNullOrEmpty(selectedObjects)) return;

            _selection.RemoveAll(s => {
                s.Refresh();
                return !s.CanBeUpdated;
            });
            
            _selection.AddRange(
                selectedObjects
                    .Where(IsPrefab)
                    .Where(go => _selection.All(s => s.Selection != go))
                    .Select(obj => new ClothingToUpdate(obj))
                    .Where(clothing => clothing.CanBeUpdated));
        }

        bool IsPrefab(GameObject obj) {
            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(obj);
            return prefabAssetType is PrefabAssetType.Regular or PrefabAssetType.Variant;
        }

        [Button, EnableIf(nameof(HasMeshForReplacement))]
        void UpdateMeshesForSelection() {
            for (int i = 0; i < _selection.Count; i++) {
                if (_selection[i].CanBeUpdated) {
                    UpdateMesh(_selection[i]);
                }
            }
        }

        static void UpdateMesh(ClothingToUpdate clothingToUpdate) {
            // Cache the data
            var magica = clothingToUpdate.Selection.GetComponentInChildren<MagicaCloth>();
            List<string> magicaTransformCache = new();
            if (magica != null) {
                magica.SerializeData.rootBones.ForEach(bone => magicaTransformCache.Add(bone.name));
            }
            
            List<string> additionalBonesCache = new();
            additionalBonesCache.AddRange(clothingToUpdate.Selection.GetComponentsInChildren<AdditionalClothBonesCatalog>()
                                                          .Select(c => c.name));
            
            File.Delete(clothingToUpdate.OldMeshPath);
            File.Move(clothingToUpdate.NewMeshPath, clothingToUpdate.OldMeshPath);
            AssetDatabase.Refresh();
            
            ReapplyCachedData(clothingToUpdate, magicaTransformCache, additionalBonesCache);
        }

        static void ReapplyCachedData(ClothingToUpdate clothingToUpdate, List<string> magicaTransformCache, List<string> additionalBonesCache) {
            // ReapplyCachedData
            var updatedCloth = clothingToUpdate.Selection;
            
            var magica = updatedCloth.GetComponentInChildren<MagicaCloth>();
            if (magica != null) {
                magica.SerializeData.rootBones.Clear();
                foreach (var bone in magicaTransformCache) {
                    Transform transform = updatedCloth.FindChildRecursively(bone);
                    if (transform == null) {
                        Log.Important?.Info($"Could not find bone {bone} in {updatedCloth.name}");
                        continue;
                    }
                    magica.SerializeData.rootBones.Add(transform);
                }
                EditorUtility.SetDirty(magica);
            }

            additionalBonesCache.ForEach(catalog => {
                var targetGO = updatedCloth.FindChildRecursively(catalog);
                if (targetGO == null) {
                    Log.Important?.Info($"Could not find space for additional bones {catalog} in {updatedCloth.name}");
                    return;
                }
                if (targetGO.GetComponent<AdditionalClothBonesCatalog>()) return;
                targetGO.gameObject.AddComponent<AdditionalClothBonesCatalog>();
            });
            
            AssetDatabase.SaveAssetIfDirty(updatedCloth);
        }
    }
}