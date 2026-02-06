using Awaken.Utility.Debugging;
using QFSW.QC;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCLoggingTools {
        [Command("logging.set", "Sets the logging filter. Provide one or more log types separated by spaces.")][UnityEngine.Scripting.Preserve]
        static void SetLogging(params LogType[] types) {
            if (types.Length == 0) {
                Log.Utils.LogType = LogType.Never;
                return;
            }
            
            LogType result = LogType.Never;
            foreach (LogType type in types) {
                result |= type;
            }
            Log.Utils.LogType = result;
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