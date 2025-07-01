using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Editor.Assets.Modding {
    public static class Modding {
        const string TempAddressableCache = "Temp/AddressablesCache";
        
        public static string ModName => "Line";

        public static string LocalBuildPath => $"Library/Mods/{ModName}";
        public static string LocalLoadPath => $"{{Awaken.TG.Assets.Modding.ModService.ModDirectoryPath}}/{ModName}";

        static void Build() {
            if (Directory.Exists(LocalBuildPath)) {
                Directory.Delete(LocalBuildPath, true);
            }
            if (Directory.Exists(Addressables.BuildPath)) {
                Directory.Move(Addressables.BuildPath, TempAddressableCache);
            }
            
            AddressableAssetSettings.BuildPlayerContent();

            File.Move($"{Addressables.BuildPath}/catalog.json", $"{LocalBuildPath}/catalog.json");
            
            if (Directory.Exists(Addressables.BuildPath)) {
                Directory.Delete(Addressables.BuildPath, true);
            }
            if (Directory.Exists(TempAddressableCache)) {
                Directory.Move(TempAddressableCache, Addressables.BuildPath);
            }
        }
    }
}