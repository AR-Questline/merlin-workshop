using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Code.Editor.Tests.Runners {
    public abstract class PlayModeTest : MonoBehaviour {
        public object data;
        protected bool _inTest;
        protected List<TestLog> _logs;
        protected List<string> _results;
        protected List<TestLogPattern> _ignoreLogs;

        protected Services Services => World.Services;
        protected abstract HashSet<UnityEngine.LogType> FailingLogs { get; }
        
        void Start() {
            DontDestroyOnLoad(this);
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            BeforeTestStarted();
            yield return SetUp();
            _inTest = true;
            yield return Test();
            _inTest = false;
            yield return TearDown();
            AfterTestEnded();
            Destroy(this);
        }

        public virtual void BeforeTestStarted() {
            Log.Important?.Info("Test Started");
            _logs = new List<TestLog>();
            _results = new List<string>();
            _ignoreLogs = new List<TestLogPattern>();
            Application.logMessageReceived += HandleLog;
        }
        
        protected virtual IEnumerator SetUp() {
            yield return null;
        }
        protected abstract IEnumerator Test();
        protected virtual IEnumerator TearDown() {
            yield return null;
        }
        
        public virtual void AfterTestEnded() {
            Application.logMessageReceived -= HandleLog;
            Log.Important?.Info("Test Ended\n--RESULTS--");
            if (_logs.Any(l => FailingLogs.Contains(l.type))) {
                Log.Important?.Error("Test failed:");
                foreach (string result in _results) {
                    Log.Important?.Error(result);
                }
                LogLogs();
            } else {
                Log.Important?.Info("Test passed positively");
                if (_logs.Any()) {
                    Log.Important?.Error("But there are additional logs");
                    LogLogs();
                }
            }
            TestRunner.OnTestEnd();
        }

        void LogLogs() {
            foreach (var log in _logs) {
                if (log.type == UnityEngine.LogType.Log) {
                    Log.Important?.Info($"{log.condition} \n {log.stacktrace}");
                } else if (log.type == UnityEngine.LogType.Assert) {
                    Debug.LogAssertion($"{log.condition} \n {log.stacktrace}");
                } else if (log.type == UnityEngine.LogType.Warning) {
                    Log.Important?.Warning($"{log.condition} \n {log.stacktrace}");
                } else if (log.type == UnityEngine.LogType.Error) {
                    Log.Important?.Error($"{log.condition} \n {log.stacktrace}");
                } else if (log.type == UnityEngine.LogType.Exception) {
                    Log.Important?.Error($"[Exception] {log.condition} \n {log.stacktrace}");
                }
            }
        }

        protected virtual void HandleLog(string condition, string stacktrace, UnityEngine.LogType type) {
            foreach (TestLogPattern logPattern in _ignoreLogs) {
                if (logPattern.Match(condition, stacktrace, type)) {
                    return;
                }
            }
            _logs.Add(new TestLog(condition, stacktrace, type));
        }
    }
}
