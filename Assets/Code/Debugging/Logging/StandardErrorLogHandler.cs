using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Debugging.Logging {
    /// <summary>
    /// Overrides the default Unity log handler to write logs only to stderr.
    /// Useful when exiting game and Unity's system are partially closed causing undefined behaviours and crashes.
    /// </summary>
    public class StandardErrorLogHandler : ILogHandler {
        readonly bool _exception;
        readonly bool _error;
        readonly bool _assert;
        readonly bool _warning;
        readonly bool _log;

        ILogHandler _previousLogHandler;
        
        public StandardErrorLogHandler(
            bool exception = true, 
            bool error = true, 
            bool assert = false, 
            bool warning = false, 
            bool log = false
        ) {
            _exception = exception;
            _error = error;
            _assert = assert;
            _warning = warning;
            _log = log;
        }

        [UnityEngine.Scripting.Preserve]
        public void Register() {
            var logger = Debug.unityLogger;
            _previousLogHandler = logger.logHandler;
            logger.logHandler = this;
        }

        [UnityEngine.Scripting.Preserve]
        public void Unregister() {
            var logger = Debug.unityLogger;
            logger.logHandler = _previousLogHandler;
            _previousLogHandler = null;
        }
        
        void ILogHandler.LogFormat(LogType logType, Object context, string format, params object[] args) {
            bool shouldLog = logType switch {
                LogType.Exception => _exception,
                LogType.Error => _error,
                LogType.Assert => _assert,
                LogType.Warning => _warning,
                LogType.Log => _log,
                _ => false
            };
            if (shouldLog) {
                LogContext(context);
                Console.Error.WriteLine(format, args);
                Console.Error.WriteLine();
                Console.Error.WriteLine();
            }
        }

        void ILogHandler.LogException(Exception exception, Object context) {
            if (_exception) {
                LogContext(context);
                int depth = 0;
                while (exception != null) {
                    Console.Error.WriteLine($"[Exception {depth++}] {exception.Message}");
                    Console.Error.WriteLine(exception.StackTrace);
                    Console.Error.WriteLine();
                    exception = exception.InnerException;
                }
                Console.Error.WriteLine();
            }
        }

        static void LogContext(Object context) {
            if (context != null) {
                Console.Error.Write("[Context: ");
                Console.Error.Write(context.ToString());
                Console.Error.WriteLine("]");
            }
        }
    }
}