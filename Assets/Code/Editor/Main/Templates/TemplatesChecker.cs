using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Editor;
using Awaken.Utility.Slack;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Awaken.TG.Editor.Main.Templates {
    public static class TemplatesChecker {
        const string SlackChannel = "jenkins";
        
        static readonly HashSet<string> WhitelistDependencies = new HashSet<string>() {

        };

        [MenuItem("TG/Templates/Check Templates")]
        public static void CheckTemplatesManually() {
            // Show dialog if should unload views
            if (EditorUtility.DisplayDialog("Unload Views",
                    "Checking templates requires unloading all views. Do you want to proceed?",
                    "Yes", "No")) {
                UnloadViews.UnloadAllView();
            }
            CheckTemplates(true);
        }

        public static void CheckTemplates(bool isManualCheck = false) {
            var errorTemplates = new List<string>();
            var loader = TemplatesLoader.CreateAndLoad();

            var invalidDependencies = new HashSet<Object>();
            var sb = new System.Text.StringBuilder();

            foreach (var template in loader.guidMap.Values) {
                var templateObject = (Object)template;
                var templatePath = AssetDatabase.GetAssetPath(templateObject);
                var dependencies = AssetDatabase.GetDependencies(templatePath, true);
                foreach (var dependencyPath in dependencies) {
                    if (WhitelistDependencies.Contains(dependencyPath)) {
                        continue;
                    }

                    // We have editor dependency resolved at a lower level
                    if (dependencyPath.Contains("/Editor/")) {
                        continue;
                    }

                    var dependency = AssetDatabase.LoadAssetAtPath<Object>(dependencyPath);
                    if (dependency is Texture or Material or Mesh or AnimationClip or VideoClip) {
                        invalidDependencies.Add(dependency);
                    } else if (dependency is GameObject go && PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Model) {
                        invalidDependencies.Add(dependency);
                    }
                }

                if (invalidDependencies.Count == 0) {
                    continue;
                }

                sb.AppendLine($"Template {template} has invalid dependencies:");
                foreach (var invalidDependency in invalidDependencies) {
                    sb.AppendLine($"  - {invalidDependency} ({AssetDatabase.GetAssetPath(invalidDependency)})");
                }
                invalidDependencies.Clear();
                string error = sb.ToString();
                Debug.LogError(error, templateObject);
                errorTemplates.Add(error);
                sb.Clear();
            }
            
            TryToSendErrorsToSlack(isManualCheck, errorTemplates);
        }

        static void TryToSendErrorsToSlack(bool isManual, List<string> errorTemplates) {
            if (isManual || errorTemplates.Count <= 0) {
                return;
            }

            var unitySynchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            try {
                var slack = new SlackMessenger(SlackChannel);
                slack.StartThread($"Checking templates failed for {errorTemplates.Count} assets. <@U05UCUH3YCD> <@U05U9ABG8SZ> <@U05U9BZR5SS>\nGit branch: {GitUtils.GetBranchName()}").Wait(10_000);
                foreach (var template in errorTemplates) {
                    slack.PostMessage(template).Wait(10_000);
                }
            } finally {
                SynchronizationContext.SetSynchronizationContext(unitySynchronizationContext);
            }
        }
    }
}