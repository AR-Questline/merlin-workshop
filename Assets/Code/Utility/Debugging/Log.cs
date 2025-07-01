using System;
using System.Collections.Generic;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.Utility.Debugging {
    [Il2CppEagerStaticClassConstruction]
    public static partial class Log {
        const string DebugLogTypeKey = "ARDebug.LogType";
        static FormattedDebugWithFilterPrefix s_genericFormattedDebug;
        static FormattedDebug s_debugFormattedDebug;
        static FormattedDebug s_minorFormattedDebug;
        static FormattedDebug s_majorFormattedDebug;
        static FormattedDebugWarning s_markingLogFormattedDebugWarning;
        static FormattedDebugCritical s_criticalFormattedDebug;
        
        static LogType s_enabledLogs = 
#if UNITY_EDITOR
            ~LogType.Debug;
#else
            Utils.BuildTypes;
#endif

        static LogType LogTypeInternal {
            get => s_enabledLogs | LogType.Critical;
            set {
                if (value == s_enabledLogs) return;
                s_enabledLogs = value;
                PlayerPrefs.SetInt(DebugLogTypeKey, (int) s_enabledLogs);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        public static void Init() {
            if (Application.isEditor && !Application.isBatchMode) {
                s_genericFormattedDebug = new FormattedDebugWithFilterPrefix("[{1}]".ColoredText(Utils.PrefixColor) + " {0}");
                s_minorFormattedDebug = new FormattedDebug($"[{nameof(LogType.Minor)}]".ColoredText(Utils.PrefixColor) + " {0}");
                s_majorFormattedDebug = new FormattedDebug($"[{nameof(LogType.Important)}]".ColoredText(Utils.PrefixColor) + " {0}");
                s_criticalFormattedDebug = new FormattedDebugCritical($"[{nameof(LogType.Critical)}]".ColoredText(new Color32(162, 29, 89, 255)) + " {0}");
                s_debugFormattedDebug = new FormattedDebug($"[{nameof(LogType.Debug)}]".ColoredText(Utils.PrefixColor) + " {0}");

            } else {
                s_genericFormattedDebug = new FormattedDebugWithFilterPrefix("[{1}] {0}");
                s_minorFormattedDebug = new FormattedDebug($"[{nameof(LogType.Minor)}] {{0}}");
                s_majorFormattedDebug = new FormattedDebug($"[{nameof(LogType.Important)}] {{0}}");
                s_criticalFormattedDebug = new FormattedDebugCritical($"[{nameof(LogType.Critical)}] {{0}}");
                s_debugFormattedDebug = new FormattedDebug($"[{nameof(LogType.Debug)}] {{0}}");
            }

            s_markingLogFormattedDebugWarning = new FormattedDebugWarning("<-----  {0}  ----->", true);

            try {
                LogTypeInternal = (LogType) PlayerPrefs.GetInt(DebugLogTypeKey, (int) LogTypeInternal);
            } catch (Exception e) {
                LogTypeInternal = Utils.BuildTypes;
                Critical?.Error("Failed to load log filter from PlayerPrefs with error below");
                Critical?.Error(e.ToString());
            }
        }
        
        /// <remark>
        /// Only use this if you want dynamic behaviour, otherwise use the static fields
        /// </remark>
        [System.Diagnostics.Contracts.Pure, CanBeNull, Obsolete("Only use this if you want dynamic behaviour, otherwise use the static fields", false)]
        public static FormattedDebugWithFilterPrefix When(LogType logType) {
#if UNITY_EDITOR
            if (logType != LogType.All && logType.HasCommonBitsFast(LogType.Marking)) Critical?.Error("Use Marking instead");
#endif

            return LogTypeInternal.HasCommonBitsFast(logType)
                       ? s_genericFormattedDebug.PassCurrentFilter(logType)
                       : null;
        }

        [CanBeNull] public static FormattedDebug Debug => LogTypeInternal.HasCommonBitsFast(LogType.Debug)
                                                              ? s_debugFormattedDebug
                                                              : null;
        [CanBeNull] public static FormattedDebug Minor => LogTypeInternal.HasCommonBitsFast(LogType.Minor)
                                                                  ? s_minorFormattedDebug
                                                                  : null;
        [CanBeNull] public static FormattedDebug Important => LogTypeInternal.HasCommonBitsFast(LogType.Important)
                                                                  ? s_majorFormattedDebug
                                                                  : null;
        [CanBeNull] public static FormattedDebugWarning Marking => LogTypeInternal.HasCommonBitsFast(LogType.Marking)
                                                                           ? s_markingLogFormattedDebugWarning
                                                                           : null;

        [CanBeNull] public static FormattedDebugCritical Critical => LogTypeInternal.HasCommonBitsFast(LogType.Critical)
                                                                 ? s_criticalFormattedDebug
                                                                 : null;
        

        public static class Utils {
            public const LogType BuildTypes = LogType.Important | LogType.Critical | LogType.Marking;
            public static readonly Color PrefixColor = new(0.2392157f, 0.4352941f, 0.7568628f);
            
            public static LogType LogType {
                get => LogTypeInternal; 
                set => LogTypeInternal = value;
            }

            public static void EDITOR_RuntimeReset() {
                Init();
            }

            public const byte HLOD_NoMaterialIndex = 1;
            public const byte PlayerJournal = 2;
            public const byte GroupPatrol = 3;
            public const byte HeroMovementInvalid = 4;
            public const byte MagicaWrongRootSetup = 5;
        }
    }
}