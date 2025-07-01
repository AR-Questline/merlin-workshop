using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Awaken.Utility.Debugging {
    public static partial class Log {
        /// <summary>
        /// Proxy class for no string concat overhead if log is filtered out. Sealed for optimization
        /// </summary>
        public sealed class FormattedDebug {
            readonly string _format;
            readonly HashSet<byte> _errorOnce = new(10);

            /// <remark>
            /// Ignoring callstack unfortunately will only work in build as unity ignores no callstack requests when custom logger is attached
            /// we would have to link this util with our EditorCustomLog that is in a different assembly :/
            /// </remark>
            public FormattedDebug(string format) {
                this._format = format;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("UNITY_EDITOR")]
            public void Info(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Log, logOption)) return;
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, logOption, context, _format, msg);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Warning(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Warning, logOption)) return;
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Warning, logOption, context, _format, msg);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Error(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Error, logOption)) return;
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Error, logOption, context, _format, msg);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ErrorThenLogs(string errorLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                
                if (!_errorOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                }
                if (EditorCallstackHandle(errorLog, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, errorLog);
            }            
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ErrorThenLogs(string errorLog, string infoLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                string logText = errorLog;
                
                if (!_errorOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                    logText = infoLog;
                }
                
                if (EditorCallstackHandle(logText, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, logText);
            }
            
            /// <summary>
            /// Returns null after first call with the same id
            /// </summary>
            [CanBeNull]
            public FormattedDebug Once(byte id) {
                if (_errorOnce.Add(id)) {
                    return this;
                }
                return null;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool EditorCallstackHandle(string msg, Object context, UnityEngine.LogType type, LogOption logOption) {
#if UNITY_EDITOR
                if (logOption == LogOption.NoStacktrace) {
                    EditorCustomLog.LogFormatNoStack(type, _format, context, msg);
                    return true;
                }
#endif
                return false;
            }
        }
        
        /// <summary>
        /// Proxy class for no string concat overhead if log is filtered out. sealed for optimization
        /// </summary>
        public sealed class FormattedDebugWarning {
            readonly string _format;
            readonly bool _noStacktraceOverride;
            readonly HashSet<byte> _warningOnce = new(10);
            
            public FormattedDebugWarning(string format, bool noStacktraceOverride = false) {
                this._format = format;
                this._noStacktraceOverride = noStacktraceOverride;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Warning(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Warning, _noStacktraceOverride ? LogOption.NoStacktrace : logOption)) return;
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Warning, _noStacktraceOverride ? LogOption.NoStacktrace : logOption, context, _format, msg);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WarningOnce(string warningLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                
                if (!_warningOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                }
                if (EditorCallstackHandle(warningLog, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, warningLog);
            }            
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WarningOnce(string warningLog, string infoLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                string logText = warningLog;
                
                if (!_warningOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                    logText = infoLog;
                }
                
                if (EditorCallstackHandle(logText, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, logText);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool EditorCallstackHandle(string msg, Object context, UnityEngine.LogType type, LogOption logOption) {
#if UNITY_EDITOR
                if (logOption == LogOption.NoStacktrace) {
                    EditorCustomLog.LogFormatNoStack(type, _format, context, msg);
                    return true;
                }
#endif
                return false;
            }
        }
        
        /// <summary>
        /// Proxy class for no string concat overhead if log is filtered out. sealed for optimization
        /// </summary>
        public sealed class FormattedDebugCritical {
            readonly string _format;
            readonly HashSet<byte> _errorOnce = new(10);
            
            public FormattedDebugCritical(string format) {
                this._format = format;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Error(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Error, logOption)) return;
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Error, logOption, context, _format, msg);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ErrorThenLogs(string errorLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                
                if (!_errorOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                }
                if (EditorCallstackHandle(errorLog, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, errorLog);
            }            
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ErrorThenLogs(string errorLog, string infoLog, byte id, Object context = null, LogOption logOption = LogOption.None) {
                UnityEngine.LogType type = UnityEngine.LogType.Error;
                string logText = errorLog;
                
                if (!_errorOnce.Add(id)) {
                    type = UnityEngine.LogType.Log;
                    logText = infoLog;
                }
                
                if (EditorCallstackHandle(logText, context, type, logOption)) return;
                UnityEngine.Debug.LogFormat(type, logOption, context, _format, logText);
            }
            
            /// <summary>
            /// Returns null after first call with the same id
            /// </summary>
            [CanBeNull]
            public FormattedDebugCritical Once(byte id) {
                if (_errorOnce.Add(id)) {
                    return this;
                }
                return null;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool EditorCallstackHandle(string msg, Object context, UnityEngine.LogType type, LogOption logOption) {
#if UNITY_EDITOR
                if (logOption == LogOption.NoStacktrace) {
                    EditorCustomLog.LogFormatNoStack(type, _format, context, msg);
                    return true;
                }
#endif
                return false;
            }
        }

        /// <summary>
        /// Proxy class for no string concat overhead if log is filtered out with additional filter prefix.
        /// </summary>
        public class FormattedDebugWithFilterPrefix {
            /// <remark>
            /// Only valid for the current invocation!!! (Not threadsafe)
            /// </remark>
            string _currentInvocationFilter;

            readonly string _format;

            public FormattedDebugWithFilterPrefix(string format) {
                _format = format;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public FormattedDebugWithFilterPrefix PassCurrentFilter(LogType type) {
                _currentInvocationFilter = type.ToStringFast();
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("UNITY_EDITOR")]
            public void Info(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Log, logOption)) return;
                UnityEngine.Debug.LogFormat(
                    UnityEngine.LogType.Log,
                    logOption,
                    context,
                    _format,
                    msg,
                    _currentInvocationFilter);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Warning(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Warning, logOption)) return;
                UnityEngine.Debug.LogFormat(
                    UnityEngine.LogType.Warning,
                    logOption,
                    context,
                    _format,
                    msg,
                    _currentInvocationFilter);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Error(string msg, Object context = null, LogOption logOption = LogOption.None) {
                if (EditorCallstackHandle(msg, context, UnityEngine.LogType.Error, logOption)) return;
                UnityEngine.Debug.LogFormat(
                    UnityEngine.LogType.Error,
                    logOption,
                    context,
                    _format,
                    msg,
                    _currentInvocationFilter);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool EditorCallstackHandle(string msg, Object context, UnityEngine.LogType type, LogOption logOption) {
#if UNITY_EDITOR
                if (logOption == LogOption.NoStacktrace) {
                    EditorCustomLog.LogFormatNoStack(type, _format, context, msg);
                    return true;
                }
#endif
                return false;
            }
        }
    }
}