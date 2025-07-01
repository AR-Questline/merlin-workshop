using System;
using System.IO;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Prefabs {
    /// <summary>
    /// Duplicates these files:
    /// Location Spec
    /// - Logic Prefab
    /// - Visual Prefab
    /// - Npc Template
    /// - - Fighting Style
    /// - - - Base Behaviours
    /// - - - Base Animations
    /// </summary>
    public class PrepareNPCSpec : OdinEditorWindow {
        [InfoBox("Base Spec from Project." +
                 "\nIt will be duplicated and all its NPC specific files will also be duplicated" +
                 "\nNew files will be created in the same folders as base files")]
        [SerializeField, OnValueChanged(nameof(UpdateBaseSpec)), ValidateInput(nameof(ValidateBasePrefab), "Put prefab from project (not hierarchy)")]
        GameObject baseSpec;
        [SerializeField, OnValueChanged(nameof(UpdateBaseSpec)), ValidateInput(nameof(ValidateVisualPrefab), "Put prefab from project (not hierarchy)")]
        GameObject visualPrefab;
        [InfoBox("\nRename data changes file names from X to Y when file is duplicated" +
                 "\nUse it to change NPC name, Tier etc.")]
        [SerializeField] RenameData[] renameData = Array.Empty<RenameData>();

        bool? _result;

        [InfoBox("Prefab prepared successfully", InfoMessageType.Warning, nameof(ShowSuccess))]
        [InfoBox("Prefab preparation failed", InfoMessageType.Error, nameof(ShowFail))]
        [ShowIf(nameof(ShowLogInfo)), ShowInInspector, Multiline(10), HideLabel]
        string _logInfo;
        
        [SerializeField, ShowIf(nameof(ShowSuccess))] GameObject result;
        
        bool ShowSuccess => _result is true;
        bool ShowFail => _result is false;
        bool ShowLogInfo => !string.IsNullOrEmpty(_logInfo);

        [MenuItem("TG/Assets/Prefabs/Prepare NPC Spec")]
        public static void ShowWindow() {
            GetWindow<PrepareNPCSpec>("Prepare NPC Prefab").Show();
        }

        void UpdateBaseSpec() {
            _result = null;
            _logInfo = null;
            result = null;
        }
        
        bool ValidateBasePrefab() {
            if (baseSpec == null || !PrefabUtility.IsPartOfPrefabAsset(baseSpec)) {
                return false;
            }
            return true;
        }
        
        bool ValidateVisualPrefab() {
            if (visualPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(visualPrefab)) {
                return false;
            }
            return true;
        }

        [Button]
        void Prepare() {
            _result = null;
            _logInfo = null;
            InfoLog($"Spec preparation started");
            var oldSpecAssetPath = AssetDatabase.GetAssetPath(baseSpec);
            var newSpec = PrefabUtility.LoadPrefabContents(oldSpecAssetPath);
            var locationSpec = newSpec.GetComponent<LocationSpec>();
            var npcAttachment = newSpec.GetComponent<NpcAttachment>();
            DuplicatePrefabReference(locationSpec);
            SetupVisualPrefab(npcAttachment);
            var addressToNpcTemplate = DuplicateNpcTemplate(npcAttachment);
            ModifyNpcTemplate(addressToNpcTemplate);
            
            // Finish
            string newSpecPath = ChangePath(oldSpecAssetPath);
            PrefabUtility.SaveAsPrefabAsset(newSpec, newSpecPath);
            PrefabUtility.UnloadPrefabContents(newSpec);
            _result = true;
            result = AssetDatabase.LoadAssetAtPath<GameObject>(newSpecPath);
            InfoLog($"Spec prepared successfully");
        }

        void DuplicatePrefabReference(LocationSpec spec) {
            var oldReference = spec.prefabReference;
            var assetPath = AssetDatabase.GUIDToAssetPath(oldReference.Address);
            var newAssetPath = DuplicateObject(assetPath, out var addressable);
            if (newAssetPath == null) {
                FixItSoonLog($"Prefab Reference not duplicated");
                return;
            }

            spec.prefabReference = addressable;
        }
        
        void SetupVisualPrefab(NpcAttachment npcAttachment) {
            ARAssetReference arAssetReference = null;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings != null) {
                AddressableAssetEntry originalEntry = AddressableHelper.GetEntry(npcAttachment.VisualPrefab);
                if (originalEntry != null) {
                    arAssetReference = AddressableHelper.MakeReference(visualPrefab, originalEntry.parentGroup.Name);
                }
            }
        
            npcAttachment.EDITOR_visualPrefab = arAssetReference;
        }

        string DuplicateNpcTemplate(NpcAttachment npcAttachment) {
            var assetPath = AssetDatabase.GUIDToAssetPath(npcAttachment.NpcTemplate.GUID);
            var addressToNewTemplate = DuplicateObject(assetPath, out var addressable);
            if (addressToNewTemplate == null) {
                FixItSoonLog($"Npc Template not duplicated");
                return null;
            }
            npcAttachment.EDITOR_npcTemplate = new TemplateReference(addressable.Address);

            return addressToNewTemplate;
        }

        void ModifyNpcTemplate(string addressToNpcTemplate) {
            var npcTemplateGo = PrefabUtility.LoadPrefabContents(addressToNpcTemplate);
            var npcTemplate = npcTemplateGo.GetComponent<NpcTemplate>();
            
            var fightingStyleAssetPath = AssetDatabase.GUIDToAssetPath(npcTemplate.FightingStyle.GUID);
            var pathToNewFightingStyle = DuplicateObject(fightingStyleAssetPath, out var addressable);
            if (pathToNewFightingStyle == null) {
                FixItSoonLog($"Fighting Style not duplicated");
                return;
            }
            ModifyFightingStyle(pathToNewFightingStyle);
            npcTemplate.EDITOR_fightingStyle = new TemplateReference(addressable.Address);
            
            PrefabUtility.SaveAsPrefabAsset(npcTemplateGo, addressToNpcTemplate);
        }

        void ModifyFightingStyle(string addressToFightingStyle) {
            var fightingStyle = AssetDatabase.LoadAssetAtPath<NpcFightingStyle>(addressToFightingStyle);
            
            var baseAssetPath = AssetDatabase.GUIDToAssetPath(fightingStyle.BaseAnimations.AssetGUID);
            var pathToNewBase = DuplicateObject(baseAssetPath, out var addressableGuid);
            fightingStyle.EDITOR_baseAnimations = new ShareableARAssetReference(addressableGuid);

            baseAssetPath = AssetDatabase.GUIDToAssetPath(fightingStyle.BaseBehaviours.AssetGUID);
            pathToNewBase = DuplicateObject(baseAssetPath, out addressableGuid);
            fightingStyle.EDITOR_baseBehaviours = new ShareableARAssetReference(addressableGuid);
        }

        string DuplicateObject(string assetPath, out ARAssetReference assetReference) {
            var newObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (newObject == null) {
                assetReference = null;
                return null;
            }
            
            if (newObject is ScriptableObject) {
                newObject = ScriptableObject.Instantiate(newObject);
            } else {
                newObject = Instantiate(newObject);
            }

            string newAssetPath = ChangePath(assetPath);
            if (newObject is GameObject newGameObject) {
                PrefabUtility.SaveAsPrefabAsset(newGameObject, newAssetPath);
            } else {
                newObject.name = Path.GetFileNameWithoutExtension(newAssetPath);
                AssetDatabase.CreateAsset(newObject, newAssetPath);
            }
            AssetDatabase.SaveAssets();
            
            DestroyImmediate(newObject);
            newObject = AssetDatabase.LoadAssetAtPath<Object>(newAssetPath);
            
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings != null) {
                AddressableAssetEntry originalEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                if (originalEntry != null) {
                    if (originalEntry.labels.Contains(TemplatesUtil.AddressableLabel) || originalEntry.labels.Contains(TemplatesUtil.AddressableLabelSO)) {
                        AddressableHelper.MakeReference(newObject, originalEntry.parentGroup.Name);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObject, out string guid, out long _);
                        assetReference = new ARAssetReference(settings.FindAssetEntry(guid).guid);
                    } else {
                        assetReference = AddressableHelper.MakeReference(newObject, originalEntry.parentGroup.Name);
                    }
                    AssetDatabase.ForceReserializeAssets(new [] {newAssetPath});
                } else {
                    assetReference = null;
                }
            } else {
                assetReference = null;
            }

            DestroyImmediate(newObject);
            return newAssetPath;
        }
        
        // Helpers

        string ChangePath(string from) {
            string to = from;
            foreach (var rename in renameData) {
                to = to.Replace(rename.from, rename.to);
            }
            var directory = Path.GetDirectoryName(to);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            to = AssetDatabase.GenerateUniqueAssetPath(to);
            const string FilePathIgnore = "Assets/Data/";
            InfoLog($"{from.Replace(FilePathIgnore, "")} => {to.Replace(FilePathIgnore, "")}");
            return to;
        }

        void FixItNowLog(string log, GameObject go = null) {
            log = $"[PrepareNPCSpec] FIX IT NOW! {log} FIX IT NOW!";
            Log.Important?.Error(log, go);
            _logInfo += $"{log}\n";
        }
        
        void FixItSoonLog(string log, GameObject go = null) {
            log = $"[PrepareNPCSpec] Please remember to fix it! {log}";
            Log.Important?.Error(log, go);
            _logInfo += $"{log}\n";
        }
        
        void DoubleCheckLog(string log, GameObject go = null) {
            log = $"[PrepareNPCSpec] Double check! {log}";
            Log.Important?.Warning(log, go);
            _logInfo += $"{log}\n";
        }

        void InfoLog(string log) {
            log = $"[PrepareNPCSpec] {log}";
            Log.Important?.Info(log);
            _logInfo += $"{log}\n";
        }

        [Serializable]
        struct RenameData {
            public string from;
            public string to;
        }
    }
}