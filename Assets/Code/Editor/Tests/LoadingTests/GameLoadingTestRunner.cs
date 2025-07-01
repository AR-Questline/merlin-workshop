using System.IO;
using UnityEngine;
using UnityEditor;

namespace Awaken.TG.Code.Editor.Tests.LoadingTests {
    public static class GameLoadingTestRunner {
        const string SavesDirectory = "SavesDatabase";

        static bool s_canContinue = false;
        
        [MenuItem("TG/Tests/Game loading (old versions vs patchers)")]
        public static void StartTestPassivesTest() {
            s_canContinue = true;
            var projectPath = ParentDirectory(Application.dataPath);
            var savesDirectory = Path.Combine(projectPath, SavesDirectory);
            var saveDirectories = Directory.GetDirectories(savesDirectory);

            int i = 0;
            Runners.TestRunner.OnInterrupted += () => s_canContinue = false;
            RunNext(saveDirectories, i);
        }

        static void RunNext(string[] saveDirectories, int index) {
            if (s_canContinue && index < saveDirectories.Length) {
                Runners.TestRunner.StartTest<GameLoadingTest>(saveDirectories[index]);
                Runners.TestRunner.OnTestEnded += () => RunNext(saveDirectories, ++index);
                Runners.TestRunner.OnInterrupted += () => s_canContinue = false;
            }
        }

        static string ParentDirectory(string directoryPath) {
            return (new DirectoryInfo(directoryPath)).Parent?.FullName;
        }
    }
}