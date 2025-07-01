using Awaken.TG.Main.Templates;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Awaken.TG.Editor.Assets.Templates {
    public class TemplateAssetsPostprocessor : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isBatchMode) {
                // We don't want to add templates on build machines, because they strip unused templates from builds.
                return;
            }
            
            for (int i = 0; i < importedAssets.Length; i++) {
                CheckAsset(importedAssets[i]);
            }
            
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            foreach (AddressableAssetGroup addressableAssetGroup in settings.groups) {
                AssetDatabase.SaveAssetIfDirty(addressableAssetGroup);
            }
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        static void CheckAsset(string path) {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            ITemplate template = TemplatesUtil.ObjectToTemplateUnsafe(asset);

            if (template != null) {
                AddressableTemplatesCreator.CreateOrUpdateAsset(asset, true);
            }
        }
    }
}