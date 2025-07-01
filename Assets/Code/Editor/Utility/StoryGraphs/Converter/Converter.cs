using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public static class Converter {

//        [MenuItem("TG/Convert/Convert location stories to template refrence")]
//        public static void ConvertLocation() {
//            var paths = GUIDMapping.Instance.GetPaths(typeof(LocationTemplate));
//            EditorUtility.DisplayProgressBar("Converting", "Start", 0);
//            int currentElement = 0;
//            int allElements = paths.Count;
//            foreach (string locationPath in paths) {
//                EditorUtility.DisplayProgressBar("Converting", $"Converting {locationPath}", currentElement++/(float)allElements);
//                using (EditPrefabAssetScope prefabAssetScope = new EditPrefabAssetScope(PrefabPath(locationPath))) {
//                    var prefab = prefabAssetScope.prefabRoot;
//                    var states = prefab.GetComponentsInChildren<LocationState>();
//                    foreach (LocationState locationState in states) {
//                        if (locationState.story?.IsSet ?? false) {
//                            locationState.storyReference = new TemplateReference(locationState.story.AssetGUID);
//                        }
//                    }
//                }
//            }
//            
//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();
//            EditorUtility.ClearProgressBar();
//        }
    }
}