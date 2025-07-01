using System.Collections.Generic;
using System.Threading;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Debugging;
using Awaken.Utility.Slack;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeLazyBaker {
        const string SlackChannel = "drake-log";

        [MenuItem("TG/Assets/Drake/Bake all marked drakes")]
        public static void BakeAll() {
            var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/3DAssets" });
            var count = prefabGUIDs.Length;
            var errorAssets = new List<string>();

            AssetDatabase.StartAssetEditing();
            try {
                for (int i = 0; i < count; i++) {
                    string prefabGUID = prefabGUIDs[i];
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                    try {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (EditorUtility.DisplayCancelableProgressBar("Baking drake", $"Baking {prefab}", i / (float)count)) {
                            break;
                        }
                        if (prefab.GetComponentsInChildren<DrakeToBake>().Length > 0) {
                            BakePrefab(prefabPath, errorAssets);
                        }
                        var lodGroups = prefab.GetComponentsInChildren<DrakeLodGroup>();
                        foreach (var lodGroup in lodGroups) {
                            var state = DrakeLodGroupEditorHelper.GetDrakeLodGroupState(lodGroup, out _);
                            if (state != DrakeLodGroupState.CorrectlyBaked) {
                                throw new System.Exception($"Drake is not correctly baked: {prefabPath}");
                            }
                        }
                    } catch (System.Exception e) {
                        Log.Minor?.Error($"For prefab with guid: {prefabGUID} can not bake drake");
                        Debug.LogException(e);
                        errorAssets.Add(prefabPath);
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();

            if (errorAssets.Count <= 0) {
                return;
            }

            var unitySynchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            try {
                var slack = new SlackMessenger(SlackChannel);
                slack.StartThread($"Drake baking failed for {errorAssets.Count} assets.").Wait(10_000);
                foreach (var errorAsset in errorAssets) {
                    slack.PostMessage(errorAsset).Wait(10_000);
                }
            } finally {
                SynchronizationContext.SetSynchronizationContext(unitySynchronizationContext);
            }
        }

        static void BakePrefab(string prefabPath, List<string> errorAssets) {
            using var editPrefab = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var drakeToBakes = editPrefab.prefabContentsRoot.GetComponentsInChildren<DrakeToBake>();
            foreach (var drakeToBake in drakeToBakes) {
                var isEditable = PrefabsHelper.IsLowestEditablePrefabStage(drakeToBake, true);
                if (!isEditable) {
                    continue;
                }
                if (DrakeToBakeEditor.IsOnValidTarget(drakeToBake)) {
                    if (!DrakeEditorHelpers.Bake(drakeToBake)) {
                        errorAssets.Add(prefabPath);
                    }
                } else {
                    errorAssets.Add(prefabPath);
                }
            }
        }
    }
}
