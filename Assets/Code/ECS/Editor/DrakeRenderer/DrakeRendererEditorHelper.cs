using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.Utils;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeRendererEditorHelper {
        public const string GroupName = "DrakeRenderer";

        static AddressableAssetSettings Settings => AddressableAssetSettingsDefaultObject.GetSettings(true);

        public static (AddressableAssetSettings, AddressableAssetGroup) GetInitialSetup(string groupName) {
            var settings = Settings;
            var group = GetGroup(settings, groupName);
            return (settings, group);
        }

        public static AddressableAssetGroup GetGroup(AddressableAssetSettings settings, string groupName) {
            for (int i = 0; i < settings.groups.Count; ++i) {
                if (settings.groups[i].Name == groupName)
                    return settings.groups[i];
            }

            var group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            return group;
        }

        public static bool ProcessEntry<T>(T asset, AddressableAssetSettings settings, AddressableAssetGroup group, out AssetReference assetReference)
            where T : Object {
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (assetPath.StartsWith("Library")) {
                assetReference = null;
                return false;
            }
            var guid = UnityEditor.AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            var mainAssetAtPath = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
            assetReference = new AssetReference(guid);
            if (mainAssetAtPath is not T) {
                assetReference.SubObjectName = asset.name;
            }

            var entry = FindAssetEntry(settings, guid);
            if (entry != null) {
                return true;
            }
            entry = settings.CreateOrMoveEntry(assetReference.AssetGUID, group, false, false);
            entry.SetAddress(entry.MainAsset.name);
            return true;
        }

        public static bool SetupRenderer(DrakeMeshRenderer drakeMeshRenderer, MeshRenderer meshRenderer,
            AddressableAssetSettings settings, AddressableAssetGroup group) {
            if (!DotsShadersUtils.AreAllMaterialsDotsShaders(meshRenderer)) {
                Log.Minor?.Error($"Materials are not DOTS materials for {drakeMeshRenderer.name}.");
                return false;
            }

            var meshFilter = meshRenderer.GetComponent<MeshFilter>();

            if (meshFilter == null) {
                Log.Minor?.Error($"There is no mesh filter for {drakeMeshRenderer.name}.");
                return false;
            }

            var mesh = meshFilter.sharedMesh;
            if (mesh == null) {
                Log.Minor?.Error($"There is no mesh for {drakeMeshRenderer.name}.");
                return false;
            }

            if (!DrakeRendererEditorHelper.ProcessEntry(mesh, settings, group, out var meshReference)) {
                Log.Minor?.Error($"Can not add mesh to addressables for {drakeMeshRenderer.name}.", mesh);
                return false;
            }

            var materials = meshRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0) {
                Log.Minor?.Error($"There is no material for {drakeMeshRenderer.name}.");
                return false;
            }
            var materialReferences = new AssetReference[materials.Length];
            for (var i = 0; i < materials.Length; i++) {
                var material = materials[i];
                if (material == null) {
                    Log.Minor?.Error($"Null material for {drakeMeshRenderer.name}.");
                    return false;
                }
                if (!DrakeRendererEditorHelper.ProcessEntry(material, settings, group, out var materialReference)) {
                    Log.Minor?.Error($"Can not add material to addressables for {drakeMeshRenderer.name}.", material);
                    return false;
                }
                materialReferences[i] = materialReference;
            }

            var lod = meshRenderer.GetComponentInParent<LODGroup>();
            DrakeLodGroup drakeLodGroup = null;
            int lodMask = 0;
            if (lod) {
                var lods = lod.GetLODs();
                var startLod = int.MaxValue;
                var endLod = -1;
                for (var i = 0; i < lods.Length; i++) {
                    foreach (var renderer in lods[i].renderers) {
                        if (renderer != meshRenderer) {
                            continue;
                        }
                        lodMask |= 1 << i;
                        startLod = math.min(startLod, i);
                        endLod = math.max(endLod, i);
                        break;
                    }
                }
                if (math.countbits(lodMask) > 0) {
                    drakeLodGroup = lod.GetComponent<DrakeLodGroup>();
                }
            }

            Transform parent = drakeMeshRenderer.transform;
            Transform child = meshRenderer.transform;

            var transformOffset = parent.worldToLocalMatrix * child.localToWorldMatrix;

            drakeMeshRenderer.Setup(meshRenderer, meshFilter, drakeLodGroup, lodMask, transformOffset, meshReference, materialReferences);
            return true;
        }
        
        public static AddressableAssetEntry FindAssetEntry(string guid) {
            return FindAssetEntry(Settings, guid);
        }

        static AddressableAssetEntry FindAssetEntry(AddressableAssetSettings settings, string guid) {
            AddressableAssetEntry entry = null;
            var groups = settings.groups;
            int i = 0;
            while (entry == null && i < groups.Count) {
                entry = groups[i].GetAssetEntry(guid);
                ++i;
            }
            return entry;
        }
    }
}
