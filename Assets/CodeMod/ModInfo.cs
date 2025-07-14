#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Scripting;

namespace Modding
{
    public class ModInfo : ScriptableObject
    {
        private static ModInfo Instance => AssetDatabase.LoadAssetAtPath<ModInfo>("Assets/ModInfo.asset") ?? throw new Exception("ModInfo asset not found. Please create one at 'Assets/ModInfo.asset'.");
        private static string AppDataLocal => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        [Preserve] public static string ModName => Instance?.codeName;
        [Preserve] public static string ModsDirectory => $"{AppDataLocal}Low\\Questline\\Fall of Avalon\\Mods";
        
        public string codeName;
        public string displayName;
        public string version = "1.0";
        public string author = "unknown";
        public string[] tags = Array.Empty<string>();

        [MenuItem("TG/Modding/Build")]
        private static void Build() 
        {
            var info = Instance;
            if (string.IsNullOrWhiteSpace(info.codeName)) {
                throw new Exception("Mod code name is not set. Please set it in the ModInfo asset.");
            }
            
            var directory = $"{ModsDirectory}\\{info.codeName}";
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
            
            AddressableAssetSettings.BuildPlayerContent();
            File.Copy($"{Addressables.BuildPath}\\catalog.json", $"{directory}\\catalog.json", true);

            var metas = new List<string>();
            if (string.IsNullOrWhiteSpace(info.displayName) == false) {
                metas.Add($"name: {info.displayName}");
            }
            if (string.IsNullOrWhiteSpace(info.version) == false) {
                metas.Add($"version: {info.version}");
            }
            if (string.IsNullOrWhiteSpace(info.author) == false) {
                metas.Add($"author: {info.author}");
            }
            if (info.tags.Length > 0) {
                metas.Add($"tags: {string.Join(' ', info.tags)}");
            }
            File.WriteAllLines($"{directory}\\mod.meta", metas);
        }
        
        [MenuItem("TG/Modding/Open Directory")]
        private static void Open()
        {
            Process.Start("explorer.exe", ModsDirectory);
        }
        
        [MenuItem("TG/Modding/Wiki")]
        private static void Wiki()
        {
            Process.Start("https://github.com/AR-Questline/merlin-workshop/wiki");
        }
    }
}
#endif