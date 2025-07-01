using Awaken.Utility.Debugging;
using QFSW.QC;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCLoggingTools {
        [Command("logging.set", "Sets the logging filter")][UnityEngine.Scripting.Preserve]
        static void SetLogging(LogType type, LogType filter2 = LogType.Never, LogType filter3 = LogType.Never, LogType filter4 = LogType.Never, LogType filter5 = LogType.Never, LogType filter6 = LogType.Never) {
            Log.Utils.LogType = type | filter2 | filter3 | filter4 | filter5 | filter6;
        }
        
        [Command("logging.get", "Gets the logging filter")][UnityEngine.Scripting.Preserve]
        static LogType GetLogging() => Log.Utils.LogType;

        [Command("logging.add", "Enable a log type")][UnityEngine.Scripting.Preserve]
        static void AddLogType(LogType newType) {
            Log.Utils.LogType |= newType;
        }
        
        [Command("logging.remove", "Disable a log type")][UnityEngine.Scripting.Preserve]
        static void RemoveLogType(LogType newType) {
            Log.Utils.LogType &= ~newType;
        }
    }
}