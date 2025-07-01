using System.IO;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Assets.ShadersPreloading {
    public static class ShadersPreloadingCommon {
        public const string FileExtension = ".graphicsstate";
        public const string ShadersTracesFolderPathInStreamingAssets = "ShadersTraces/GraphicsStateCollections";

        const string FileNamePrefix = "ShadersTrace";

        public static string GetFileName(RuntimePlatform runtimePlatform, GraphicsDeviceType graphicsDeviceType) {
            var fileName = string.Concat(FileNamePrefix, "_", runtimePlatform.ToString(), "_", graphicsDeviceType.ToString());
            return fileName;
        }

        public static string GetFileNameWithExtension(RuntimePlatform runtimePlatform, GraphicsDeviceType graphicsDeviceType) {
            var fileName = string.Concat(GetFileName(runtimePlatform, graphicsDeviceType), FileExtension);
            return fileName;
        }
        
        public static bool TryFindMatchingCollectionInStreamingAssets(RuntimePlatform runtimePlatform, GraphicsDeviceType graphicsDeviceType, out GraphicsStateCollection collection, bool logErrorIfNotFound) {
            var shadersTracesFolderPathInStreamingAssets = Path.Combine(Application.streamingAssetsPath, ShadersTracesFolderPathInStreamingAssets);
            if (Directory.Exists(shadersTracesFolderPathInStreamingAssets) == false) {
                collection = null;
                if (logErrorIfNotFound) {
                    Log.Important?.Error($"There is no directory {shadersTracesFolderPathInStreamingAssets} with shaders traces");
                }
                return false;
            }
            var searchedFileName = ShadersPreloadingCommon.GetFileNameWithExtension(runtimePlatform, graphicsDeviceType);
            string matchingFilePath = null;
            foreach (var filePath in Directory.EnumerateFiles(shadersTracesFolderPathInStreamingAssets, "*" + ShadersPreloadingCommon.FileExtension, SearchOption.AllDirectories)) {
                if (Path.GetFileName(filePath) == searchedFileName) {
                    matchingFilePath = filePath;
                    break;
                }
            }
            if (matchingFilePath == null) {
                collection = null;
                if (logErrorIfNotFound) {
                    Log.Important?.Error($"There is no {nameof(GraphicsStateCollection)} in StreamingAssets/{ShadersPreloadingCommon.ShadersTracesFolderPathInStreamingAssets} folder matching {runtimePlatform} and {graphicsDeviceType}");
                }
                return false;
            }
            collection = new GraphicsStateCollection(matchingFilePath);
            collection.name = Path.GetFileNameWithoutExtension(searchedFileName);
            return collection.runtimePlatform == runtimePlatform && collection.graphicsDeviceType == graphicsDeviceType;
        }


#if UNITY_EDITOR
        public static void EDITOR_UpdateCollectionsList(ref GraphicsStateCollection[] collections) {
            string[] collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(GraphicsStateCollection)}");
            collections = new GraphicsStateCollection[collectionGUIDs.Length];
            for (int i = 0; i < collections.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);
                collections[i] = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
            }
        }
#endif
    }
}