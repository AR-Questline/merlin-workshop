using System;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.Utility;
using UnityEngine;
using Directory = System.IO.Directory;

namespace Awaken.TG.Debugging.Logging {
    public static class LogsCollector {
        const string ExceptionsPopupOnceArgument = "exceptionsPopupWarningOnce";
        const string ExceptionsPopupArgument = "exceptionsPopupWarning";
        public static string LogsPath => Path.Combine(Application.persistentDataPath, "Logs");
        
        static readonly LogType[] AllowedTypes = {LogType.Exception, LogType.Error, LogType.Warning};
#if !UNITY_PS5 || UNITY_EDITOR
        static FileStream s_fileStream;
#endif
        static StreamWriter s_streamWriter;
        static readonly StringBuilder StringBuilder;

        static readonly CircularList<int> LoopList = new CircularList<int>(5);
        static bool s_inLoop = false;
        static bool s_exceptionThrown = false;
        static readonly bool ExceptionsPopupWarningOnce = false;
        static readonly bool ExceptionsPopupWarning = false;
        static readonly bool DoNotCheckLoops = false;

        public static void Init() { } // Just to make sure the static constructor is called

        static LogsCollector() {
            StringBuilder = new StringBuilder();

            AttachLogMessageCallback(HandleLogWithInitialChecks);

            ExceptionsPopupWarningOnce = Environment.GetCommandLineArgs().Any(arg => arg.Contains(ExceptionsPopupOnceArgument));
            ExceptionsPopupWarning = Environment.GetCommandLineArgs().Any(arg => arg.Contains(ExceptionsPopupArgument));
            DoNotCheckLoops = Configuration.GetBool("logs.donotcheckloops", false);
            
            Cleanup();
        }

        public static List<KeyValuePair<string, DateTime>> GetLogNamesWithDates(bool withInvalids = false) {
            var logsPath = LogsPath;
            if (Directory.Exists(logsPath)) {
                var files = Directory.GetFiles(logsPath).ToList();
                var fileNames = files.Select(Path.GetFileNameWithoutExtension);
                List<KeyValuePair<string, DateTime>> logs = new List<KeyValuePair<string, DateTime>>();
                foreach (var file in fileNames) {
                    DateTime? date = DateTimeUtil.StringToDateTime(file);
                    if (date.HasValue) {
                        logs.Add(new KeyValuePair<string, DateTime>(file, date.Value));
                    } else if (withInvalids) {
                        logs.Add(new KeyValuePair<string, DateTime>(file, new DateTime(1980, 1, 1)));
                    }
                }

                return logs;
            }

            return new List<KeyValuePair<string, DateTime>>();
        }

        public static void Dispose() {
            DetachLogMessageCallback(HandleLog);
            DetachLogMessageCallback(HandleLogWithInitialChecks);
            s_streamWriter?.Dispose();
#if !UNITY_PS5
            s_fileStream?.Dispose();
#endif
        }

        static bool StartFileStream() {
            var directoryPath = LogsPath;
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            var fileName = DateTimeUtil.DateTimeToString(DateTime.Now);
            var path = Path.Combine(directoryPath, $"{fileName}.txt");
            try {
#if UNITY_PS5 && !UNITY_EDITOR
                s_streamWriter = new StreamWriter(Console.OpenStandardOutput());
#else
                s_fileStream = new FileStream(path, FileMode.CreateNew);
                s_streamWriter = new StreamWriter(s_fileStream);
#endif
                s_streamWriter.AutoFlush = true;
            } catch (Exception e) {
                Console.WriteLine("Failed to create log file: " + e.Message + "\n   stacktrace:" + e.StackTrace);
                return false;
            }

            return true;
        }

        static void HandleLogWithInitialChecks(string log, string stacktrace, LogType type) {
#if UNITY_EDITOR && !UNITY_PS5
            if (!Application.isPlaying) {
                return;
            }
#endif

            DetachLogMessageCallback(HandleLogWithInitialChecks);
            if (StartFileStream()) {
                AttachLogMessageCallback(HandleLog);
                HandleLog(log, stacktrace, type);
            }
        }

        static void HandleLog(string log, string stacktrace, LogType type) {
            PopupIfNeeded(type, log, stacktrace);

            if (AllowedTypes.Contains(type)) {
                if (CheckLoop(log, stacktrace, type)) {
                    return;
                }

                PopupV2IfNeeded(type, log, stacktrace);

                if (s_inLoop) {
                    s_streamWriter.WriteLine("---- Entered loop with log ----");
                    s_streamWriter.WriteLine(log);
                    s_streamWriter.WriteLine();
                    return;
                }

                StringBuilder.Clear();
                StringBuilder.Append('[');
                StringBuilder.Append(DateTime.Now.ToString(CultureInfo.InvariantCulture));
                StringBuilder.Append("][");
                StringBuilder.Append(type.ToStringFast());
                StringBuilder.Append("] - ");
                StringBuilder.Append(log);
                StringBuilder.AppendLine();
                StringBuilder.Append(stacktrace);

                s_streamWriter.WriteLine(StringBuilder.ToString());
                s_streamWriter.WriteLine();
            }
        }

        static void PopupIfNeeded(LogType type, string log, string stacktrace) {
            if (ExceptionsPopupWarningOnce && !s_exceptionThrown && type == LogType.Exception) {
                s_exceptionThrown = true;
                var reportingStackTrace = GetFirstTwoLines(stacktrace);
                StacktracePopup(log + "\n" + stacktrace, reportingStackTrace);
            }
        }
        
        static void PopupV2IfNeeded(LogType type, string log, string stacktrace) {
            if (ExceptionsPopupWarning && type == LogType.Exception) {
                var reportingStackTrace = GetFirstTwoLines(stacktrace);
                StacktracePopup(log + "\n" + stacktrace, reportingStackTrace);
            }
        }
        
        static void StacktracePopup(string exception, string reportingStackTrace) {
            PopupUI.SpawnNoChoicePopup(
                viewType: typeof(VSmallPopupNoTransitionsUI),
                callback: () => CopyToClipboard("```csharp\n" + exception + "\n```"),
                text: $"Send your logs to programmers\n{reportingStackTrace}",
                title: "Thrown Exception!");
        }
        
        static void CopyToClipboard(string stacktrace) {
            GUIUtility.systemCopyBuffer = stacktrace;
        }

        static string GetFirstTwoLines(string stacktrace) => stacktrace.Split('\n')[0];

        static bool CheckLoop(string log, string stacktrace, LogType type) {
            if (DoNotCheckLoops) {
                return false;
            }
            var hash = Hash(log, stacktrace, type);
            if (LoopList.Has(hash)) {
                LoopList.MakeLast(hash);
                if (!s_inLoop) {
                    s_inLoop = true;
                    return false;
                }
                return true;
            }
            s_inLoop = false;
            LoopList.Add(hash);
            return false;
        }

        static int Hash(string log, string stacktrace, LogType logType) {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + log.GetHashCode();
                if (logType == LogType.Exception) {
                    hash = hash * 23 + stacktrace.GetHashCode();
                }
                hash = hash * 23 + logType.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Remove old logs, to cleanup players space
        /// </summary>
        static void Cleanup() {
            List<KeyValuePair<string, DateTime>> logs = GetLogNamesWithDates(withInvalids: true);

            static bool IsOlderThanXDays(KeyValuePair<string, DateTime> log) => DateTime.Now - log.Value > TimeSpan.FromDays(3);

            foreach (var log in logs.Where(IsOlderThanXDays)) {
                string fileName = $"{log.Key}.txt";
                string path = Path.Combine(LogsPath, fileName);
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
        }

        static void AttachLogMessageCallback(Application.LogCallback callback) {
#if UNITY_PS5 && !UNITY_EDITOR
            Application.logMessageReceivedThreaded += callback;
#else
            Application.logMessageReceived += callback;
#endif
        }

        static void DetachLogMessageCallback(Application.LogCallback callback) {
#if UNITY_PS5 && !UNITY_EDITOR
            Application.logMessageReceivedThreaded -= callback;
#else
            Application.logMessageReceived -= callback;
#endif
        }

        class CircularList<T> {
            T[] _list;
            int _current;
            int _max;
            
            public CircularList(int capacity) {
                _list = new T[capacity];
                _current = 0;
                _max = capacity;
            }

            public bool Has(T item) {
                return _list.Contains(item);
            }

            public void MakeLast(T item) {
                var toSwapIndex = Array.IndexOf(_list, item);
                _current = Index(1);
                var tmp = _list[_current];
                _list[_current] = _list[toSwapIndex];
                _list[toSwapIndex] = tmp;
            }

            public void Add(T item) {
                _current = Index(1);
                _list[_current] = item;
            }

            int Index(int index) {
                return (_current + index) % _max;
            }
        }
    }
}