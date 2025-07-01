using System.Collections.Generic;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Awaken.Utility.Debugging {
    public static class InitLoggerProxy {
        static List<(LogType, string)> s_queuedLogs = new();

        public static void QueueLog(string message, LogType logType = LogType.All) {
            if (s_queuedLogs == null) {
                Log.When(logType)?.Error(message);
                return;
            }
            s_queuedLogs.Add((logType, message));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void TriggerQueuedLogs() {
            if (s_queuedLogs == null) return;
            for (int i = 0; i < s_queuedLogs.Count; i++) {
                Log.When(s_queuedLogs[i].Item1)?.Error(s_queuedLogs[i].Item2);
            }
            s_queuedLogs = null;
        }
    }
}