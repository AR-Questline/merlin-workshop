using System;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using DG.Tweening.Plugins.Core.PathCore;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Awaken.TG.Editor.Assets.Modding {
    public class ModExporter : OdinEditorWindow {
        [SerializeField, TableList(IsReadOnly = true)] List<Result> results = new();
        
        [Button, HorizontalGroup("Collect")]
        void CollectAll() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            results.Clear();
            foreach (var group in AddressableHelper.Settings.groups) {
                if (group.name == "Scenes") {
                    continue;
                }
                if (group.name == "Templates.Stories") {
                    continue;
                }
                if (group.name == "Templates.Skills") {
                    continue;
                }
                if (group.name.StartsWith("Localization")) {
                    continue;
                }
                foreach (var entry in group.entries) {
                    results.Add(new Result(entry));
                }
            }
            results.Sort((lhs, rhs) => string.Compare(lhs.directory, rhs.directory, StringComparison.Ordinal));
            Log.Important?.Info($"ModExporter.Collect: {stopwatch.Elapsed}");
        } 
           
        [Button, HorizontalGroup("Collect")]
        void CollectTemplates() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            results.Clear();
            foreach (var group in AddressableHelper.Settings.groups) {
                if (!group.name.StartsWith("Templates.")) {
                    continue;
                }
                if (group.name == "Templates.Story") {
                    continue;
                }
                if (group.name == "Templates.Skills") {
                    continue;
                }
                if (group.name == "Templates.Quests") {
                    continue;
                }
                foreach (var entry in group.entries) {
                    results.Add(new Result(entry));
                }
            }
            results.Sort((lhs, rhs) => string.Compare(lhs.directory, rhs.directory, StringComparison.Ordinal));
            Log.Important?.Info($"ModExporter.Collect: {stopwatch.Elapsed}");
        }

        [Button, HorizontalGroup("Export")]
        void MapGuids() {
            string path = EditorUtility.SaveFilePanel("Mod Exporter", "", "guids.txt", "txt");
            using var file = new FileStream(path, FileMode.Create);
            using var writer = new StreamWriter(file);
            foreach (var result in results) {
                writer.Write(AssetDatabase.AssetPathToGUID(result.path));
                writer.Write(',');
                writer.Write(result.path);
                writer.Write('\n');
            }
        }

        [Button, HorizontalGroup("Export")]
        void Select() {
            var objects = new Object[results.Count];
            for (int i = 0; i < results.Count; i++) {
                objects[i] = results[i].asset;
            }
            Selection.objects = objects;
        }
        
        [MenuItem("TG/Modding/Exporter", priority = 100)]
        public static void Open() {
            GetWindow<ModExporter>().Show();
        }

        [Serializable, InlineProperty]
        struct Result {
            [HideInInspector] public string path;
            [DisplayAsString] public string directory;
            public Object asset;

            public Result(AddressableAssetEntry entry) {
                path = entry.AssetPath;
                directory = Path.GetDirectoryName(path);
                asset = entry.MainAsset;
            }
        }
    }
}