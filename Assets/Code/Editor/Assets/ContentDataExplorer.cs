using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class ContentDataExplorer : OdinEditorWindow {
        const string ResourcesDirectory = "Assets/Resources/Data";

        [TableList, SerializeField] List<TemplateData> enemies;
        [TableList, SerializeField] List<TemplateData> locations;
        [TableList, SerializeField] List<TemplateData> npcs;

        // === Initialization
        [MenuItem("TG/Assets/Content Explorer")]
        public static void ShowWindow() {
            var window = GetWindow<ContentDataExplorer>();
            window.titleContent = new GUIContent("Content Data Explorer");
        }

        protected override void OnEnable() {
            enemies = new List<TemplateData>();
            locations = new List<TemplateData>();
            npcs = new List<TemplateData>();
        }

        protected override void OnImGUI() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Collect Data")) {
                CollectData();
            }

            if (enemies.Any() && GUILayout.Button("Export Data")) {
                Export();
            }
            GUILayout.EndHorizontal();
            base.OnImGUI();
        }

        void CollectData() {
            EditorUtility.DisplayProgressBar("Collecting Data", "Getting Templates & Prefabs", 0f);
            enemies = new List<TemplateData>();
            locations = new List<TemplateData>();
            npcs = new List<TemplateData>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] {ResourcesDirectory});
            int done = 0;
            float count = guids.Length;
            foreach (var guid in guids) {
                GetTemplateDataFromGuid(guid);
                done++;
                EditorUtility.DisplayProgressBar("Collecting Data", $"Getting Templates & Prefabs: {done}/{count}", done/count);
            }
            EditorUtility.ClearProgressBar();
        }

        void GetTemplateDataFromGuid(string guid) {
            GameObject prefab = null;
            try {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                LocationTemplate locationTemplate = prefab.GetComponent<LocationTemplate>();
                if (locationTemplate != null && locationTemplate.GetComponent<LocationSpec>() != null) {
                    LocationSpec spec = locationTemplate.GetComponent<LocationSpec>();
                    if (TagUtils.HasRequiredTag(spec.Tags, "rogue:NPC")) {
                        npcs.Add(new TemplateData(guid, locationTemplate, spec.prefabReference));
                    } else if (TagUtils.HasRequiredKind(spec.Tags, "rogue")) {
                        locations.Add(new TemplateData(guid, locationTemplate, spec.prefabReference));
                    }
                }
            } catch (Exception e) {
                Log.Important?.Error(e.ToString());
            }

            if (prefab != null) {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
        
        void Export() {
            var pathToSave = EditorUtility.SaveFolderPanel("Choose save file location", "", "");
            SaveToFile(enemies, "Enemies");
            SaveToFile(locations, "Locations");
            SaveToFile(npcs, "NPCs");

            void SaveToFile(List<TemplateData> data, string csvName) {
                // using var stream = File.OpenWrite(Path.Combine(pathToSave, $"{csvName}.csv"));
                // using var writer = new StreamWriter(stream);
                // using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                // csv.Context.RegisterClassMap<TemplateDataMap>();
                // csv.WriteRecords(data);
            }
        }
        //
        // // === Helper Classes
        // [UsedImplicitly]
        // sealed class TemplateDataMap : ClassMap<TemplateData> {
        //     public TemplateDataMap() {
        //         Map(m => m.templateName).Index(0).Name("Template Name");
        //         Map(m => m.prefabName).Index(1).Name("Prefab Name");
        //     }
        // }
    }

    [Serializable]
    public class TemplateData {
        string baseGUID;
        string prefabGUID;
        
        [ShowInInspector, DisplayAsString(false), VerticalGroup("Template")]
        public string templateName { get; private set; }
        [ShowInInspector, DisplayAsString(false), VerticalGroup("Prefab")]
        public string prefabName { get; private set; }
        
        public TemplateData(string guid, Template template, ARAssetReference prefabReference) {
            baseGUID = guid;
            templateName = template.name;
            prefabGUID = prefabReference.Address;
            var prefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GUIDToAssetPath(prefabReference.Address));
            prefabName = prefab.name;
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        [Button("Ping"), VerticalGroup("Template")]
        void Ping() {
            EditorGUIUtility.PingObject(AssetsUtils.LoadAssetByGuid<Object>(baseGUID));
        }

        [Button("Ping"), VerticalGroup("Prefab")]
        void PingPrefab() {
            EditorGUIUtility.PingObject(AssetsUtils.LoadAssetByGuid<Object>(prefabGUID));
        }
    }
}
