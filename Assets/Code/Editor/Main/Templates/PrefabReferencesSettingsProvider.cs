using UnityEditor;

namespace Awaken.TG.Editor.Main.Templates {
    public static class PrefabReferencesSettingsProvider {
        [SettingsProvider]
        public static SettingsProvider CreateProviderFromSettingsObject() {
            var provider = AssetSettingsProvider.CreateProviderFromObject("Project/Prefab References Settings", PrefabReferencesSettings.Instance);
            provider.keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(PrefabReferencesSettings.Instance));
            return provider;
        }
    }
}