using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

// ReSharper disable InconsistentNaming

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    public interface IMapScene : IScene {
        const string SceneSoundsMusicPostfix = "Music";
        const string SceneSoundsAmbiencePostfix = "Ambience";
        const string SceneSoundsMapItemsPostfix = "MapItems";

        Func<bool> TryRestoreWorld { get; set; }
        SceneInitializer SceneInitialization { get; }
        bool InitializationCanceled { get; set; }
        Scene[] UnityScenes { get; }

        [UnityEngine.Scripting.Preserve]
        public static string[] GetMapSoundBanksNames(string sceneName) {
            string[] banksNames = {
                sceneName + SceneSoundsMusicPostfix,
                sceneName + SceneSoundsAmbiencePostfix,
                sceneName + SceneSoundsMapItemsPostfix
            };
            return banksNames;
        }

        public static void GetMapSoundBanksNames(string sceneName, List<string> banksNames) {
            banksNames.Add(sceneName + SceneSoundsMusicPostfix);
            banksNames.Add(sceneName + SceneSoundsAmbiencePostfix);
            banksNames.Add(sceneName + SceneSoundsMapItemsPostfix);
        }

        public UniTaskVoid FailAndReturnToTitleScreen(string message = "");
    }
}