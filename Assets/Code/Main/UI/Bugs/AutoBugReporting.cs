using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Awaken.TG.Debugging;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Debugging.Logging;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Saving.Utils;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.TitleScreen.FileVerification;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Awaken.Utility.Threads;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using Unity.Services.Core;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.UI.Bugs {
    public partial class AutoBugReporting : Model {
        const int LastSavesCount = 3;

        static readonly UserReportingClientConfiguration ReportingConfiguration = new(
            maximumEventCount: 100,
            maximumMeasureCount: 300,
            framesPerMeasure: 60,
            maximumScreenshotCount: 1,
            metricsGatheringMode: MetricsGatheringMode.Automatic);
        
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === Fields & Properties
        protected bool IsCreatingUserReport { get; set; }
        protected bool _isSubmitting;
        
        string _summary, _description;

        public static bool SendAutoReport(string summary, string description) {
#if UNITY_EDITOR || UNITY_GAMECORE || UNITY_PS5
            return false;
#else
            if (World.HasAny<AutoBugReporting>()) {
                return false;
            }

            World.Add(new AutoBugReporting(summary, description));
            return true;
#endif
        }
        
        public static void SendReportIfNotMainThread() {
            if (!ThreadSafeUtils.IsMainThread) {
                Log.Important?.Error("BUILD - Called single thread api not from main thread");
                SendAutoReport("THREAD ERROR", "Called single thread api not from main thread");
            }
        }
        
        protected AutoBugReporting() {}

        [UnityEngine.Scripting.Preserve]
        public AutoBugReporting(string summary, string description) {
            _summary = summary;
            _description = description;
        }
        
        protected override void OnFullyInitialized() {
            CreateUserReportWhenReady().Forget();
            ReConfigure();
        }

        async UniTaskVoid CreateUserReportWhenReady() {
            await UniTask.WaitUntil(() => UnityServices.State == ServicesInitializationState.Initialized);
            CreateUserReport().Forget();
        }

        public async UniTaskVoid CreateUserReport() {
            if (IsCreatingUserReport) {
                return;
            }

            IsCreatingUserReport = true;
            
            await UniTask.NextFrame();
            TakeScreenshot(128, 128);                
            await UniTask.Delay(1000, true);

            CreateAttachments();
            UserReportingService.Instance.CreateNewUserReport(() => {
                CreateReport(_summary, _description);                
            });
        }

        protected static void TakeScreenshot(int width, int height) {
            UserReportingService.Instance.TakeScreenshot(width, height);
        }

        protected static void CreateAttachments() {
            var dateTimePrefix = DateTimePrefix();
            AddSavesAttachments(dateTimePrefix);
            AddLogsAttachments(dateTimePrefix);
            AddUserSettingsAttachment(dateTimePrefix);
        }
        
        protected void CreateReport(string summary, string description) {
            _description = description;
            _summary = summary;

#if !UNITY_GAMECORE && !UNITY_PS5
            if (ApplicationFileIntegrityChecker.Instance is { Success: false }) {
                _summary = "[FAILED INTEGRITY] " + _summary;
            }
#endif
            
            if (Application.isEditor) {
                _summary = "[EDITOR] " + _summary;
            }
            
            _summary = $"[{Application.version}v] {_summary}";
            
            if (!Application.isEditor && (Services.TryGet<GameplayMemory>()?.Context()?.Get<bool>(CheatController.CheatsMemoryLabel) ?? false)) {
                _summary = "[CHEATS ENABLED] " + _summary;
            }
            
            _summary = _summary.Replace('.', ' ');
            
            AddDimensions();
            AddDescriptions();
            
            IsCreatingUserReport = false;

            SubmitUserReport();
        }

        static void AddSavesAttachments(string filePrefix, int savesCount = LastSavesCount) {
            string domainPath = Domain.Main.ConstructSavePath(null);
            var savePath = Path.Combine(CloudService.Get.DataPath, domainPath);
            if (Directory.Exists(savePath)) {
                string savesZipName = filePrefix + "_saves.zip";
                string zipFilePath = Path.Combine(savePath, savesZipName);
                if (File.Exists(zipFilePath)) {
                    File.Delete(zipFilePath);
                }

                string saveSlotsPath = savePath;
                string[] globals = Array.Empty<string>();
                IEnumerable<string> lastSaveFiles = Enumerable.Empty<string>();

                if (Directory.Exists(saveSlotsPath)) {
                    var lastSaveFilesNames = GetLastSaveFiles(savesCount);
                    globals = Directory.GetFiles(saveSlotsPath, "*.*", SearchOption.TopDirectoryOnly);
                    lastSaveFiles = Directory
                        .GetDirectories(saveSlotsPath)
                        .Where(d => lastSaveFilesNames.Contains(Path.GetFileName(d)))
                        .SelectMany(dir => Directory.GetFiles(dir, "*.*"));
                }

                var files = Directory
                    .GetFiles(savePath, "*.*", SearchOption.TopDirectoryOnly)
                    //.AppendWith(globals)
                    //.AppendWith(lastSaveFiles)
                    .Where(f => !f.Contains("_uncompressed"));

                try {
                    IOUtil.CreateZipFile(zipFilePath, files);
                    UserReportingService.Instance.AddAttachmentToReport("Saves", savesZipName, File.ReadAllBytes(zipFilePath), "application/zip");
                    if (File.Exists(zipFilePath)) {
                        File.Delete(zipFilePath);
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        static string[] GetLastSaveFiles(int savesCount) {
            return World.All<SaveSlot>().Select(s => s).OrderByDescending(s => s.LastSavedTime).Take(savesCount).Select(s => s.ID).ToArray();
        }

        static void AddLogsAttachments(string filePrefix) {
#if !UNITY_GAMECORE && !UNITY_PS5
            List<KeyValuePair<string, DateTime>> logs = LogsCollector.GetLogNamesWithDates();
            if (logs.Any()) {
                logs.Sort((log1, log2) => log2.Value.CompareTo(log1.Value));
                string logsZipName = filePrefix + "_logs.zip";
                string zipFilePath = Path.Combine(LogsCollector.LogsPath, logsZipName);     
                if (File.Exists(zipFilePath)) {
                    File.Delete(zipFilePath);
                }

                ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
                
                for (int i = 0; i < (logs.Count > 3 ? 3 : logs.Count); i++) {
                    var log = logs[i];
                    string fileName = $"{log.Key}.txt";
                    string path = Path.Combine(LogsCollector.LogsPath, fileName);

                    try {
                        byte[] bytes = ReadBytesFromLogFile(path);
                        ZipArchiveEntry zipEntry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
                        using Stream stream = zipEntry.Open();
                        stream.Write(bytes);
                    } catch (Exception e) {
                        Log.Important?.Error($"Failed to attach log: {logs[i].Value}\n{e.Message}");
                    }
                }
                
                zip.Dispose();
                
                UserReportingService.Instance.AddAttachmentToReport("Logs", logsZipName, File.ReadAllBytes(zipFilePath), "application/zip");
                if (File.Exists(zipFilePath)) {
                    File.Delete(zipFilePath);
                }
            }
#endif
        }

        static void AddUserSettingsAttachment(string filePrefix) {
            string settingsPath = Path.Combine(CloudService.Get.DataPath, CloudService.UnsynchronizedSavedGamesDirectory);
            if (Directory.Exists(settingsPath)) {
                string settingsFilePath = Directory.GetFiles(settingsPath, "*.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (settingsFilePath == null) {
                    return;
                }

                try {
                    UserReportingService.Instance.AddAttachmentToReport("Settings", filePrefix + "_settings.txt", File.ReadAllBytes(settingsFilePath),
                        "text/plain");
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Can read file without needing permission to edit
        /// </summary>
        static byte[] ReadBytesFromLogFile(string path) {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] bytes = new byte[fs.Length];
            int numBytesToRead = (int)fs.Length;
            int numBytesRead = 0;
            while (numBytesToRead > 0) {
                int n = fs.Read(bytes, numBytesRead, numBytesToRead);
                // Break when the end of the file is reached.
                if (n == 0) {
                    break;
                }

                numBytesRead += n;
                numBytesToRead -= n;
            }
            fs.Close();
            return bytes;
        }

        static void AddDimensions() {
            string platform = Application.platform.ToString();
            string version = Application.version;
            UserReportingService.Instance.AddDimensionValue("Platform", platform);
            UserReportingService.Instance.AddDimensionValue("Version", version);
            UserReportingService.Instance.AddDimensionValue("Platform.Version", $"{platform}.{version}");
            UserReportingService.Instance.AddDimensionValue("Language", Application.systemLanguage.ToString());

            UserReportingService.Instance.AddMetadata("QualityLevel", World.Only<GraphicPresets>().ActivePreset.EnumName);
        }

        void AddDescriptions(string reportSpecificInfo = "") {
            // Set Summary
            UserReportingService.Instance.SetReportSummary(_summary);

            // Set Description
            UserReportingService.Instance.SetReportDescription(_description + reportSpecificInfo);
        }

        protected void SubmitUserReport() {
            if (_isSubmitting || !UserReportingService.Instance.HasOngoingReport) {
                return;
            }
            _isSubmitting = true;
            SendReport();
        }

        void SendReport(int retryCount = 0) {
            // Send Report
            UserReportingService.Instance.SendUserReport(OnSendProgress, success => {
                if (!success && retryCount < 5) {
                    // try once again
                    ResendReport(retryCount);
                } else {
                    OnSendResult(success);
                }
            });
        }
        
        protected virtual void OnSendProgress(float progress) {}
        protected virtual void OnSendResult(bool success) {
            _isSubmitting = false;
            ReConfigure();
            Discard();
        }

        void ResendReport(int retryCount) {
            retryCount++;

            ReConfigureWithMetricsSaved();

            var dateTimePrefix = DateTimePrefix();

            if (retryCount < 2) {
                // first retry, without screenshots
                AddSavesAttachments(dateTimePrefix);
            }

            if (retryCount == 2) {
                // second retry, with only 1 save and without screenshots
                AddSavesAttachments(dateTimePrefix, 1);
            }

            if (retryCount < 4) {
                // third retry, without saves and screenshots
                AddLogsAttachments(dateTimePrefix);
            }

            if (retryCount < 5) {
                // fourth retry, without logs, saves and screenshots
                AddUserSettingsAttachment(dateTimePrefix);
            }
            // fifth retry, without settings, logs, saves and screenshots

            UserReportingService.Instance.CreateNewUserReport(
                () => {
                    AddDimensions();
                    string retryInfo = "\n\nRETRY INFO: "
                                       + retryCount switch {
                                           1 => "Stripped screenshots.",
                                           2 => "Sent only one save.",
                                           3 => "Stripped saves.",
                                           4 => "Stripped saves and logs.",
                                           5 => "Stripped everything !!!",
                                           _ => "RetryCount: " + retryCount
                                       };

                    AddDescriptions(retryInfo);

                    SendReport(retryCount);
                });
        }

        static string DateTimePrefix() {
            return DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
        }

        static void ReConfigureWithMetricsSaved() {
            Type managerType = typeof(UserReportingService).Assembly.GetType("Unity.Services.UserReporting.UserReportingManager", true);
            PropertyInfo currentClientProperty = managerType?.GetProperty("CurrentClient", BindingFlags.Static | BindingFlags.NonPublic);
            var currentClient = currentClientProperty?.GetValue(null);
            var clientMetricsField = currentClient?.GetType().GetField("ClientMetrics", BindingFlags.Instance | BindingFlags.NonPublic);
            var currentMetricsField = currentClient?.GetType().GetField("CurrentMetrics", BindingFlags.Instance | BindingFlags.NonPublic);
            var measuresField = currentClient?.GetType().GetField("Measures", BindingFlags.Instance | BindingFlags.NonPublic);
            var clientMetrics = clientMetricsField?.GetValue(currentClient);
            var currentMetrics = currentMetricsField?.GetValue(currentClient);
            var measures = measuresField?.GetValue(currentClient);

            //Configure resets all report data
            ReConfigure();

            currentClient = currentClientProperty?.GetValue(null);
            clientMetricsField?.SetValue(currentClient, clientMetrics);
            currentMetricsField?.SetValue(currentClient, currentMetrics);
            measuresField?.SetValue(currentClient, measures);
        }

        /// <summary>
        /// Configure resets all report data
        /// </summary>
        public static void ReConfigure() {
            UserReportingService.Instance.Configure(ReportingConfiguration);
        }
    }
}