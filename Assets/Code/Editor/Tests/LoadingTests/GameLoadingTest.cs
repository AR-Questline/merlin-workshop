using System.Collections;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.Code.Editor.Tests.Runners;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Saving.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Code.Editor.Tests.LoadingTests {
    public class GameLoadingTest : PlayModeTest {
        bool _failedToLoad = false;

        static HashSet<LogType> s_failingLogs = new HashSet<LogType>() {
            LogType.Exception
        };
        protected override HashSet<LogType> FailingLogs => s_failingLogs;

        protected override IEnumerator SetUp() {
            yield return base.SetUp();
            // Open title
            yield return LoadTitleScreen();
            yield return new WaitForSeconds(0.5f);
            // Replace current save data
            // SaveSlot saveSlot = World.Add(new SaveSlot("AutoLoadingTest"));
            //
            // var savePath = Path.Combine(
            //     CloudService.Get.DataPath, Domain.Main.ConstructSavePath(saveSlot));
            // if (Directory.Exists(savePath)) {
            //     Directory.Delete(savePath, true);
            // }
            //
            // IOUtil.DirectoryCopy((string) data, savePath);
        }

        IEnumerator LoadTitleScreen() {
            SceneManager.LoadScene(TitleScreenLoading.SceneName, LoadSceneMode.Single);
            yield return TestUtils.WaitForWorld();
        }

        protected override IEnumerator Test() {
            SaveSlot saveSlot = World.All<SaveSlot>().FirstOrDefault(s => s.Name == "AutoLoadingTest");
            TitleScreenUtils.LoadGame(saveSlot);
            while (SceneManager.GetActiveScene().name != "Rogue") {
                yield return null;
            }
            while(!_failedToLoad && !FullyLoaded() ){
                yield return null;
            }

            yield return null;
        }

        static bool FullyLoaded() {
            return SceneGlobals.Scene is MapScene { IsInitialized: true };
        }

        protected override void HandleLog(string condition, string stacktrace, LogType type) {
            if (type == LogType.Error) {
                if (condition.Equals("Corrupted save file")) {
                    _logs.Add(new TestLog($"Test failed: {data}", "", LogType.Exception));
                    _failedToLoad = true;
                } else {
                    _logs.Add(new TestLog($"Encountered no loading error - {data} - {condition}", stacktrace, LogType.Warning));
                }
            } else if (type == LogType.Exception) {
                _logs.Add(new TestLog($"Encountered no loading error - {data} - {condition}", stacktrace, LogType.Warning));
                _failedToLoad = true;
            }
        }
    }
}