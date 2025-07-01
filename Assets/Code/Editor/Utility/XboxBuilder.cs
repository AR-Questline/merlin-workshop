using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
#if UNITY_GAMECORE
using UnityEditor.GameCore;
#endif

namespace Awaken.TG.Editor.Utility {
    public class XboxBuilder {
        const string PluginPath = @"Assets\Vendor\GDK";
        const string PackageDirectory = "Package";
        const string OutputDirectory = "Loose";

        static string s_buildLooseOutputFolderPath;
        static string s_buildOutputFolderPathAbsolute;
        static string s_buildPackageOutputFolderPath;

        string _contentIdOverride;
        string _originalSquare150X150LogoPath;
        string _originalSquare44X44LogoPath;
        string _originalSquare480X480LogoPath;
        string _originalSplashScreenImagePath;
        string _originalStoreLogoPath;
        BuildTarget _target;


        string ConsoleCodeName => _target == BuildTarget.GameCoreXboxSeries ? "Scarlett" : "XboxOne";

#if UNITY_GAMECORE
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
#if UNITY_GAMECORE
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
        
        static bool ChooseOutputFolder(string relativePath) {
            s_buildOutputFolderPathAbsolute = Path
                .Join(Directory.GetParent(Application.dataPath)?.ToString(), "\\", relativePath + "\\")
                .Replace("/", "\\");
            // Create two subfolders underneath. One for the Loose build and another for the Package.
            s_buildLooseOutputFolderPath = s_buildOutputFolderPathAbsolute + OutputDirectory;
            s_buildPackageOutputFolderPath = s_buildOutputFolderPathAbsolute + PackageDirectory;

            Directory.CreateDirectory(s_buildLooseOutputFolderPath);

            return true;
        }

        bool CopyManifestFiles() {
            string square150X150LogoPath = string.Empty;
            string square44X44LogoPath = string.Empty;
            string square480X480LogoPath = string.Empty;
            string splashScreenImagePath = string.Empty;
            string storeLogoPath = string.Empty;

            string gameConfigFilePath = GetGameConfigPath();

            // Use the first MicrosoftGame.Config
            if (!string.IsNullOrEmpty(gameConfigFilePath)) {
                XDocument gameConfigXmlDoc = XDocument.Load(gameConfigFilePath);
                try {
                    string imagesPath = GetGameConfigPath().Replace("MicrosoftGame.Config", string.Empty);
                    XElement executableEl = (from executable in gameConfigXmlDoc.Descendants("Executable")
                        select executable).First();
                    executableEl.SetAttributeValue("Name", PlayerSettings.productName.Replace(" ", "") + ".exe");
                    executableEl.SetAttributeValue("TargetDeviceFamily", ConsoleCodeName);

                    // Find the images
                    XElement shellVisualsEl = (from shellVisual in gameConfigXmlDoc.Descendants("ShellVisuals")
                        select shellVisual).First();

                    XAttribute square480X480LogoAttribute = shellVisualsEl.Attribute("Square480x480Logo");
                    _originalSquare480X480LogoPath = square480X480LogoAttribute.Value;
                    square480X480LogoPath = (imagesPath + _originalSquare480X480LogoPath).Replace("/", "\\");

                    XAttribute square150X150LogoAttribute = shellVisualsEl.Attribute("Square150x150Logo");
                    _originalSquare150X150LogoPath = square150X150LogoAttribute.Value;
                    square150X150LogoPath = (imagesPath + _originalSquare150X150LogoPath).Replace("/", "\\");

                    XAttribute square44X44LogoAttribute = shellVisualsEl.Attribute("Square44x44Logo");
                    _originalSquare44X44LogoPath = square44X44LogoAttribute.Value;
                    square44X44LogoPath = (imagesPath + _originalSquare44X44LogoPath).Replace("/", "\\");

                    XAttribute splashScreenImageAttribute = shellVisualsEl.Attribute("SplashScreenImage");
                    _originalSplashScreenImagePath = splashScreenImageAttribute.Value;
                    splashScreenImagePath = (imagesPath + _originalSplashScreenImagePath).Replace("/", "\\");

                    XAttribute storeLogoAttribute = shellVisualsEl.Attribute("StoreLogo");
                    _originalStoreLogoPath = storeLogoAttribute.Value;
                    storeLogoPath = (imagesPath + _originalStoreLogoPath).Replace("/", "\\");

                    // Check for a Content ID override
                    XElement contentIdOverrideEl = (from contentIdOverride in gameConfigXmlDoc.Descendants("ContentIdOverride")
                        select contentIdOverride).First();
                    _contentIdOverride = contentIdOverrideEl.Value;

                    gameConfigXmlDoc.Save(gameConfigFilePath);
                } catch (Exception ex) {
                    Log.Important?.Error("Error: Invalid or corrupt MicrosoftGame.Config. Check exception thrown below");
                    UnityEngine.Debug.LogException(ex);
                }
            } else {
                Log.Important?.Error("Error: No Microsoft.GameConfig found. You can create one under by selecting the GDK > Associate with the Store.");
            }

            List<string> storeAssetsToCopy = new List<string>();
            storeAssetsToCopy.Add(gameConfigFilePath);
            storeAssetsToCopy.Add(square150X150LogoPath);
            storeAssetsToCopy.Add(square44X44LogoPath);
            storeAssetsToCopy.Add(square480X480LogoPath);
            storeAssetsToCopy.Add(splashScreenImagePath);
            storeAssetsToCopy.Add(storeLogoPath);

            foreach (string storeAssetToCopy in storeAssetsToCopy) {
                string fileName = Path.GetFileName(storeAssetToCopy);
                string fullSourcePath = storeAssetToCopy;
                string destinationPath = s_buildLooseOutputFolderPath + "\\" + fileName;
                if (!string.IsNullOrEmpty(fullSourcePath)) {
                    File.Copy(fullSourcePath, destinationPath, true);
                }
            }
            
            return true;
        }

        bool PostBuild() {
            string[] files = Directory.GetFiles(s_buildLooseOutputFolderPath, "MicrosoftGame.Config",
                SearchOption.TopDirectoryOnly);
            string gameConfigFilePath = files[0];
            string cleanedGameConfigFilePath = gameConfigFilePath.Replace("/", "\\");

            XDocument gameConfigXmlDoc = XDocument.Load(cleanedGameConfigFilePath);
            XElement shellVisualsEl = (from shellVisual in gameConfigXmlDoc.Descendants("ShellVisuals")
                select shellVisual).First();

            // We need to rewrite the manifest to point at where the images will be placed
            // in the build directory.
            shellVisualsEl.SetAttributeValue("Square150x150Logo", Path.GetFileName(_originalSquare150X150LogoPath));
            shellVisualsEl.SetAttributeValue("Square44x44Logo", Path.GetFileName(_originalSquare44X44LogoPath));
            shellVisualsEl.SetAttributeValue("Square480x480Logo", Path.GetFileName(_originalSquare480X480LogoPath));
            shellVisualsEl.SetAttributeValue("SplashScreenImage", Path.GetFileName(_originalSplashScreenImagePath));
            shellVisualsEl.SetAttributeValue("StoreLogo", Path.GetFileName(_originalStoreLogoPath));

            gameConfigXmlDoc.Save(cleanedGameConfigFilePath);

            return true;
        }

        bool CreateLayoutFile() {
            string arguments = string.Format("/c makepkg.exe genmap /f \"{0}\\layout.xml\" /d \"{0}\"",
                s_buildLooseOutputFolderPath);
            return StartCmdProcess(arguments);
        }

        bool MakePackage() {
            Directory.CreateDirectory(s_buildPackageOutputFolderPath);
            
            string makePkgExtraArgs = string.Empty;
            if (!string.IsNullOrEmpty(_contentIdOverride)) {
                makePkgExtraArgs = "/contentid " + _contentIdOverride;
            }

            string gameOsFile = $"{s_buildLooseOutputFolderPath}\\GameOS.Xvd";
            string arguments = string.Format(
                "/c makepkg.exe pack /f \"{0}\\layout.xml\" /d \"{0}\" /pd \"{1}\" /gameos \"{2}\" /CorrelationId Unity " + makePkgExtraArgs,
                s_buildLooseOutputFolderPath, s_buildPackageOutputFolderPath, gameOsFile);
            if (BuildTools.HasArgument("submission")) {
                arguments += " /l";
                /* Info about /l: "Submission licenses should only be used for a package that is submitted to certification.
                Please ensure you build your package without the /L parameter to enable sideloading of the package."*/
            } else {
                arguments += " /skipvalidation";
            }
            return StartCmdProcess(arguments);
        }

        [MenuItem("TG/Build/Xbox deploy", false, 0)]
        static bool DeployPackage() {
#if UNITY_GAMECORE
            ChooseOutputFolder(BuildTools.BuildPaths[BuildTarget.GameCoreXboxSeries].BuildDirectory);
            string packagePath = Directory
                .EnumerateFiles(s_buildPackageOutputFolderPath)
                .Where(f => Path.GetExtension(f) == ".xvc")
                .OrderByDescending(f => Directory.GetLastWriteTime(f).Ticks)
                .FirstOrDefault();

            string gdkPath = GDKRegistry.GetInstalledGDKPath().Replace("/", "\\");
            string gdkAppsPath = Path.Combine(gdkPath, "bin");
            string targetAddress = Environment
                .GetCommandLineArgs()
                .FirstOrDefault(arg => arg.StartsWith("ip:"))
                ?.Substring(3);

            if (targetAddress.IsNullOrWhitespace() && FindDeviceAddress(gdkAppsPath, out string deviceAddress)) {
                targetAddress = deviceAddress;
            } else {
                Log.Critical?.Error("Could not find any Xbox device.");
                return false;
            }

            targetAddress = targetAddress != null ? "/x:" + targetAddress + " " : "";
            string processArgs = $"{targetAddress} install {packagePath}";
            int exitCode = RunXbappProcess(gdkAppsPath, processArgs);
            Log.Critical?.Error($"Deploy finished with exit code {exitCode}");
            return exitCode == 0;
#endif
            return false;
        }

        static bool FindDeviceAddress(string gdkAppsPath, out string deviceAddress) {
            using var process = new Process();
            string xbappPath = Path.Combine(gdkAppsPath, "xbconnect.exe");
            process.StartInfo = new ProcessStartInfo(xbappPath, "/discover /B");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            bool canceled = false;
            while (!process.WaitForExit(100) && !canceled) {
                canceled = EditorUtility.DisplayCancelableProgressBar("Searching for devices", "Please wait...", 0);
            }

            if (!canceled) {
                Regex ipRegex = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                MatchCollection matches = ipRegex.Matches(process.StandardOutput.ReadToEnd());

                foreach (Match match in matches) {
                    string ip = match.Value;
                    if (IPAddress.TryParse(ip, out _)) {
                        deviceAddress = ip;
                        process.Close();
                        EditorUtility.ClearProgressBar();
                        return true;
                    }
                }
            }

            process.Close();
            EditorUtility.ClearProgressBar();
            deviceAddress = string.Empty;
            return false;
        }

        static int RunXbappProcess(string gdkAppsPath, string args) {
            using var process = new Process();
            string xbappPath = Path.Combine(gdkAppsPath, "xbapp.exe");
            process.StartInfo = new ProcessStartInfo(xbappPath, args);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            bool canceled = false;
            while (!process.WaitForExit(100) && !canceled) {
                canceled = EditorUtility.DisplayCancelableProgressBar("Deploying to Xbox", "Please wait...", 0);
            }

            if (canceled) {
                process.Kill();
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            if (!standardOutput.IsNullOrWhitespace()) {
                Log.Important?.Info(standardOutput);
            }

            EditorUtility.ClearProgressBar();
            int exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        bool OpenBuildOutputFolder() {
            Process.Start("explorer.exe", s_buildOutputFolderPathAbsolute);
            return true;
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

        static bool StartCmdProcess(string arguments) {
            bool succeeded = true;

            string workingDirectory = @"C:\Program Files (x86)\Microsoft GDK\bin";

            if (!Directory.Exists(workingDirectory)) {
                Log.Important?.Error("Error: Could not find the GDK tools. Make sure you have the Microsoft GDK installed.");
                return false;
            }

            var processStartInfo = new ProcessStartInfo("cmd.exe");
            processStartInfo.Arguments = arguments;
            processStartInfo.WorkingDirectory = workingDirectory;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            try {
                var process = Process.Start(processStartInfo);
                string standardOutputMessage = string.Empty;
                string standardErrorMessage = string.Empty;
                while (!process.StandardOutput.EndOfStream) {
                    string standardOutputLine = process.StandardOutput.ReadLine();
                    standardOutputMessage += standardOutputLine;
                    if (standardOutputLine.Contains("Install failed")) {
                        succeeded = false;
                    }
                }

                while (!process.StandardError.EndOfStream) {
                    string standardErrorLine = process.StandardError.ReadLine();
                    standardErrorMessage += standardErrorLine;
                }

                process.WaitForExit();
                process.Close();
                if (!string.IsNullOrEmpty(standardOutputMessage)) {
                    Log.Important?.Info(standardOutputMessage);
                }

                if (!string.IsNullOrEmpty(standardErrorMessage)) {
                    Log.Important?.Error(standardErrorMessage);
                    succeeded = false;
                }
            } catch (Exception e) {
                Log.Important?.Error(e.Message);
                succeeded = false;
            }

            return succeeded;
        }
    }
}