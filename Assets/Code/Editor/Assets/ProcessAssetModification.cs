using System.IO;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.Heroes.Skills.Graphs;
using Awaken.TG.Main.Stories.Core;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class ProcessAssetModification : AssetModificationProcessor {
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options) {
            try {
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (asset is StoryGraph s) {
                    foreach (var node in s.nodes.OfType<StoryNode>()) {
                        foreach (var element in node.elements) {
                            LocalizationUtils.RemoveAllLocalizedTerms(element, s.StringTable);
                        }
                        LocalizationUtils.RemoveAllLocalizedTerms(node, s.StringTable);
                    }
                    EditorUtility.SetDirty(s.StringTable);
                    AssetDatabase.SaveAssets();
                } else if (asset is GameObject go && !AssetDatabase.IsForeignAsset(go)) {
                    LocalizationTools.RemoveAllStringTableEntriesFromPrefab(assetPath);
                }

                if (Directory.Exists(assetPath)) {
                    Directory.Delete(assetPath, true);
                } else {
                    File.Delete(assetPath);
                }

                if (File.Exists(assetPath + ".meta")) {
                    File.Delete(assetPath + ".meta");
                }
                return AssetDeleteResult.DidDelete;
            }
            catch (UnityException e) {
                Log.Important?.Error(e.Message);
                return AssetDeleteResult.FailedDelete;
            }
        }
    }
}
