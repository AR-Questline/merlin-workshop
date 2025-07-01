using Awaken.TG.Editor.SimpleTools;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Awaken.TG.Debugging.Logging;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static Awaken.TG.Editor.Utility.BuildTools;
using Debug = UnityEngine.Debug;
#if UNITY_PS5
using UnityEditor.PS5;
using PlayerSettingsPS5 = UnityEditor.PS5.PlayerSettings;
#endif

namespace Awaken.TG.Editor.Utility {
    internal static class PS5Builder {
        public static int GetSubtarget(bool scriptsOnly) {
#if UNITY_PS5
            if (scriptsOnly) {
                return (int)UnityEditor.PS5.PS5BuildSubtarget.PCHosted;
            }

            return (int)UnityEditor.PS5.PS5BuildSubtarget.GP5Project;
#endif
            return 0;
        }

        [MenuItem("TG/Build/Set PS5 Settings")]
        public static void SetPS5BuildOptions() {
#if UNITY_PS5
            PlayerSettingsPS5.configFileParsed = true; //May be related to ProjectSettings/PS5Settings.json
            //Set up in params.json or paramsDemo.json
            PlayerSettingsPS5.appType = 0; //applies to either Application DRM Type or Category (GameApplication, AdditionalContent)
            PlayerSettingsPS5.supportsVR = false; //The rest looks like VR related, but couldn't find where to set them in editor
            PlayerSettingsPS5.requiresVR = false;
            PlayerSettingsPS5.psvr2PlayAreaSupport = PS5PSVR2PlayAreaSupport.Both;
            PlayerSettingsPS5.psvr2DetailedGazeTrackingStatusEnabled = false;
            PlayerSettingsPS5.psvr2SeeThroughEnabled = false;
            PlayerSettingsPS5.psvr2HandTrackingEnabled = false;
            
            //Build Options
            PlayerSettingsPS5.buildSubtarget = PS5BuildSubtarget.GP5Project;
            PlayerSettingsPS5.hardwareTarget = PS5HardwareTarget.TrinityAndStandard;
            PlayerSettingsPS5.workspaceName = "workspace0"; //when subtarget is set to GP5 or PCHosted
            PlayerSettingsPS5.buildCompressionType = PS5BuildCompressionType.Default;
            PlayerSettingsPS5.buildCompressionLevel = 0;
            PlayerSettingsPS5.keepPackageFiles = true; // for Package subtarget
            
            //Project Settings -> Resolution and Presentation
            PlayerSettingsPS5.videoOutPixelFormat = 0; //Color Depth
            PlayerSettingsPS5.videoOutStandardModeInitialWidth = 2560; //Resolution
            PlayerSettingsPS5.videoOutInitialWidth = 3840;
            PlayerSettingsPS5.useResolutionFallback = true; //1080p Fallback
            PlayerSettingsPS5.videoOutOutputMode = 1; //Target Frame Rate (1 = 60Hz, 15 = 120Hz) 
            PlayerSettingsPS5.vrrSupport = PS5VRRSupport.EnabledTypeA; //Variable Refresh Rate
            
            //Project Settings -> Splash
            PlayerSettingsPS5.operatingSystemCanDisableSplashScreen = true; //Notice Screen Skip Flag skips Splash
            
            //Project Settings -> Other
            PlayerSettingsPS5.monoEnv = "";
            PlayerSettingsPS5.enableApplicationExit = false;
            PlayerSettingsPS5.playerPrefsSupport = false;
            PlayerSettingsPS5.playerPrefsMaxSize = 1024;
            PlayerSettingsPS5.saveDataImagePath = "ProjectSettings/SAVE_ICON.png";
            PlayerSettingsPS5.sdkOverride = "";
            PlayerSettingsPS5.restrictedAudioUsageRights = false;
            
            //Project Settings -> Publishing Settings
            PlayerSettingsPS5.resetTempFolder = true;
            PlayerSettingsPS5.bgmPath = "ProjectSettings/snd0.at9";
            PlayerSettingsPS5.backgroundImagePath = "ProjectSettings/BACKGROUND_SONYPS5.png";
            PlayerSettingsPS5.startupBackgroundImagePath = "ProjectSettings/BACKGROUND_SONYPS5.png";
            PlayerSettingsPS5.startupForegroundImagePath = "ProjectSettings/FOREGROUND_SONYPS5.png";
            PlayerSettingsPS5.startupImagesFolder = "";
            PlayerSettingsPS5.iconImagesFolder = "";
            PlayerSettingsPS5.disableAutoHideSplash = false;
            PlayerSettingsPS5.sharedBinarySystemFolders = new string[0];
            PlayerSettingsPS5.sharedBinaryContentLabels = new string[0];
            PlayerSettingsPS5.includedModules = PS5SDKTools.BuildSDKModuleList().ToArray();

            if (BuildTools.IsDemo()) {
                PlayerSettingsPS5.passcode = "vyaIspeYv88_30lUis8hTw0UlTbLVvOs";
                PlayerSettingsPS5.paramFilePath = "ProjectSettings/paramDemo.json";
                PlayerSettingsPS5.npConfigZipPath = "ProjectSettings/npconfig_PPSA28613_00_latest_20250321060820.zip";
            } else {
                PlayerSettingsPS5.passcode = "LqZZmaSAak81uFlUKlpxtv9yfx0n9P1H";
                PlayerSettingsPS5.paramFilePath = "ProjectSettings/param.json";
                PlayerSettingsPS5.npConfigZipPath = "ProjectSettings/npconfig_PPSA19411_00_Cert_20250424133401.zip";
            }
#endif
        }

        public static void SetPS5BuildOptionsForScriptsOnly() {
            SetPS5BuildOptions();
#if UNITY_PS5
            PlayerSettingsPS5.buildSubtarget = PS5BuildSubtarget.PCHosted;
#endif
        }

        [MenuItem("TG/Build/Create PS5 Package/For submission")]
        static void CreatePS5PackageForSubmission() => CreatePackage(BuildPaths[BuildTarget.PS5].BuildPath, null, true);

        [MenuItem("TG/Build/Create PS5 Package/Development")]
        static void CreatePS5PackageForDevelopment() => CreatePackage(BuildPaths[BuildTarget.PS5].BuildPath, null, false);

        public static bool CreatePackage(string buildPath, BuildReport report, bool forSubmission) {
#if UNITY_PS5
            using var progressBar = ProgressBar.Create("Creating PS5 package");
            progressBar.Display(0, "Searching for GP5 project...");
            var gp5Files = Directory.GetFiles(buildPath, "*.gp5");
            
            if (gp5Files.Length == 0) {
                Log.Critical?.Error($"No GP5 files found in {buildPath}");
                return false;
            } else if (gp5Files.Length > 1) {
                Log.Important?.Warning($"More than one GP5 files found in {buildPath}. Package will be created from first one: {gp5Files[0]}");
            }

            var args = new List<string>();
            if (forSubmission) {
                args.Add("--for_submission");
            }

            var outputPath = $"{buildPath}/FallOfAvalon_{DateTime.UtcNow.ToString("s").Replace(":", "_")}.pkg";
            var stdOut = new string[0];

            progressBar.Display(0.5f, "Building package...");
            bool result = PS5PackageTools.ImgCreate(gp5Files[0], args, outputPath, false, ref stdOut);

            foreach (var log in stdOut) {
                Debug.Log(log);
            }

            return result;
#else
            return false;
#endif
        }

        [MenuItem("TG/Build/PS5 - deploy", false, 0)]
        public static bool DeployPS5Package() {
#if UNITY_PS5
            if(!HasArgument("deploy")) {
                return true;
            }

            BuildPathOption buildPath = BuildPaths[BuildTarget.PS5];
            string packagePath = Directory
                .EnumerateFiles($"{Application.dataPath}/../{buildPath.BuildPath}"
                    .Replace("/", @"\")
                    .Replace(@"\\", @"\"))
                .Where(f => f.EndsWith(".pkg"))
                .OrderByDescending(f => File.GetLastWriteTime(f).Ticks)
                .FirstOrDefault();

            if (packagePath.IsNullOrWhitespace()) {
                Log.Critical?.Error("No package found to deploy");
                return false;
            }

            packagePath = $"\"{packagePath}\"";
            string targetArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("-ip:"));
            string target;
            bool hasTarget;
            if (targetArg.IsNullOrWhitespace()) {
                hasTarget = TryFindTarget(out target);
            } else {
                target = targetArg[4..];
                hasTarget = true;
            }

            string args = $"package install {packagePath}" + (hasTarget ? $" /target:{target}" : "");
            using var progressBar2 = ProgressBar.Create("\"PS5: deploying package\"", "...");
            string tool = PS5SDKTools.GetTool("prospero-ctrl");
            var process = new Process {
                StartInfo = new ProcessStartInfo(tool, args) {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            bool canceled = false;
            while (!process.WaitForExit(100) && !canceled) {
                canceled = progressBar2.DisplayCancellable(1f);
            }

            if (canceled) {
                process.Kill();
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            if (!standardOutput.IsNullOrWhitespace()) {
                Log.Critical?.Error(standardOutput);
            }

            string standardError = process.StandardError.ReadToEnd();
            if (!standardError.IsNullOrWhitespace()) {
                Log.Critical?.Error(standardError);
            }

            int exitCode = process.ExitCode;
            process.Close();
            return !canceled || exitCode == 0;
#else
            return false;
#endif
        }

        static bool TryFindTarget(out string target) {
            using var progressBar = ProgressBar.Create("PS5: searching for target", "...");
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("prospero-ctrl", "target find") {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            bool canceled = false;
            var time = DateTime.Now;
            while (!process.WaitForExit(100) && !canceled) {
                float progress = (float)((DateTime.Now - time).TotalSeconds / 5.177f);
                canceled = progressBar.DisplayCancellable(progress);
            }

            if (canceled) {
                process.Kill();
                target = string.Empty;
                return false;
            }

            target = process.StandardOutput
                .ReadToEnd()
                .Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(entry => entry.StartsWith("- Host: "))
                ?.Substring(8)
                .Trim();

            return !target.IsNullOrWhitespace();
        }
    }
}