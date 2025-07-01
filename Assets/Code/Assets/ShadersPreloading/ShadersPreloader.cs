using System;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Awaken.TG.Assets.ShadersPreloading {
    public class ShadersPreloader {
        const string ShaderVariantCollectionFolderPath = "ShadersTraces/ShaderVariantCollections";
        const string LastPreloadedBuildVersion = "last.shaders.preloaded.version";
        const string DoNotPreloadKey = "do_not_preload_shaders";
        const string ForcePreloadShadersKey = "force_preload_shaders";
        const string PreloadInEditor = "preload_in_editor";

        public static ShadersPreloader Instance { get; private set; }

        public static bool ShouldPreload() {
            var forcePreloadShaders = Configuration.GetBool(ForcePreloadShadersKey);
            if (forcePreloadShaders) {
                return true;
            }

            var forceNotPreloadShaders = Configuration.GetBool(DoNotPreloadKey);
            if (forceNotPreloadShaders) {
                return false;
            }

            if (PlatformUtils.IsEditor) {
                return Configuration.GetBool(PreloadInEditor);
            }

            if (PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck) {
                return false;
            }

            return PlayerPrefs.GetString(LastPreloadedBuildVersion, string.Empty) != GitDebugData.BuildCommitHash;
        }

        public static ShaderVariantCollection[] TryGetShaderVariantCollectionsToPreload() {
            var matchingCollections = Resources.LoadAll<ShaderVariantCollection>(ShaderVariantCollectionFolderPath);
            if (matchingCollections == null || matchingCollections.Length == 0) {
                Log.Important?.Error($"There is no {nameof(ShaderVariantCollection)} in resources {ShaderVariantCollectionFolderPath}");
                matchingCollections = Array.Empty<ShaderVariantCollection>();
            }
            return matchingCollections;
        }
        
        public static GraphicsStateCollection[] TryGetGraphicsStateCollectionsToPreload() {
            var platform = Application.platform;
            var graphicsDeviceType = SystemInfo.graphicsDeviceType;
            if (ShadersPreloadingCommon.TryFindMatchingCollectionInStreamingAssets(platform, graphicsDeviceType, out var matchingCollection, true) && matchingCollection.totalGraphicsStateCount > 0) {
                return new[] { matchingCollection };
            }
            return Array.Empty<GraphicsStateCollection>();
        }
        
        public static void MarkPreloaded() {
            PlayerPrefs.SetString(LastPreloadedBuildVersion, GitDebugData.BuildCommitHash);
        }
    }
}