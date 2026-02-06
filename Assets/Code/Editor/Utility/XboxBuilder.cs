using System.IO;
using System.Linq;
using System.Xml.Linq;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
using Awaken.TG.Main.SocialServices.MicrosoftServices;
using UnityEditor.GameCore;
#endif

namespace Awaken.TG.Editor.Utility {
    public class XboxBuilder {
        const string PluginPath = @"Assets\Vendor\GDK";
        
        BuildTarget _target;

        string ConsoleCodeName => _target == BuildTarget.GameCoreXboxSeries ? "Scarlett" : "XboxOne";

#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
        public static GameCoreBuildSubtarget GetGameCoreSubtarget() {
            if (BuildTools.HasArgument("debug")) {
                return GameCoreBuildSubtarget.Development;
            } else {
                return GameCoreBuildSubtarget.Master;
            }
        }
#else
        public static int GetGameCoreSubtarget() => 0;
#endif

        public static bool SetGameCoreBuildSettings() {
#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
            var gameCoreScarlettSettings = GameCoreScarlettSettings.GetInstance();
            gameCoreScarlettSettings.InitialiseSettings();

            gameCoreScarlettSettings.BuildSubtarget = GetGameCoreSubtarget();
            gameCoreScarlettSettings.DeploymentMethod = GameCoreDeployMethod.Package;
            gameCoreScarlettSettings.PackageEncryption = BuildTools.HasArgument("submission")
                ? GameCorePackageEncryption.Submission
                : GameCorePackageEncryption.Development;

            gameCoreScarlettSettings.ApplyAnyChanges();
#endif
            return true;
        }

        public static bool SetGameCoreBuildSettingsForScriptsOnly() {
#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
            var gameCoreScarlettSettings = GameCoreScarlettSettings.GetInstance();
            gameCoreScarlettSettings.InitialiseSettings();

            gameCoreScarlettSettings.BuildSubtarget = GetGameCoreSubtarget();
            gameCoreScarlettSettings.DeploymentMethod = GameCoreDeployMethod.Push;
            gameCoreScarlettSettings.ApplyAnyChanges();
#endif
            return true;
        }
        
        public static void PrepareScid(string microsoftGameConfigPath) {
            Debug.LogFormat(UnityEngine.LogType.Log, LogOption.NoStacktrace, null, $"Extracting SCID from {microsoftGameConfigPath}...");
            string microsoftManagerScript = $"{Application.dataPath}/Code/Main/SocialServices/MicrosoftServices/MicrosoftManager.cs";
            var gameConfigDoc = XDocument.Load(microsoftGameConfigPath);
            var scid = gameConfigDoc
                .Descendants("ExtendedAttribute")
                .First(node => node.Attribute("Name") is { Value: "Scid" })
                .Attribute("Value")?.Value;

            if (string.IsNullOrEmpty(scid)) {
                Log.Important?.Error("Error: Scid not found in MicrosoftGame.Config. Please ensure it is set.");
                return;
            }

            if (!File.Exists(microsoftManagerScript)) {
                Log.Important?.Error($"Error: MicrosoftManager.cs not found at path {microsoftManagerScript}.");
                return;
            }

            File.WriteAllText(microsoftManagerScript, File.ReadAllText(microsoftManagerScript).Replace("@@{scid}@@", scid));
            Debug.LogFormat(UnityEngine.LogType.Log, LogOption.NoStacktrace, null, $"SCID set to {scid} in MicrosoftManager.cs.");
        }

        public static string GetGameConfigPath() {
            string getGameConfigPath = string.Empty;
            try {
                // First look in the place where the MicrosoftGame.Config should be.
                string path = $@"{PluginPath}\GDK-Tools\ProjectMetadata";
                string[] files = Directory.GetFiles(path, "MicrosoftGame.Config", SearchOption.TopDirectoryOnly);
                // If not found, do a more expensive operation to search the entire project directory.
                if (files.Length == 0) {
                    files = Directory.GetFiles(Application.dataPath, "MicrosoftGame.Config", SearchOption.AllDirectories);
                }

                if (files.Length > 0) {
                    getGameConfigPath = files[0];
                }

                getGameConfigPath = getGameConfigPath.Replace("/", "\\");
            } catch {
                Log.Important?.Error("MicrosoftGame.config not found.");
            }

            return getGameConfigPath;
        }
    }
}