using System;
using System.Linq;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Threads;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCSettingsControl {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [Command("set.affinity", "Sets the process affinity to the specified core(s)")][UnityEngine.Scripting.Preserve]
        static void SetProcessAffinity(params int[] coreIds) {
            if (coreIds.IsNullOrEmpty() || Environment.ProcessorCount < coreIds.Max() || coreIds.Any(id => id < 0)) {
                QuantumConsole.Instance.LogToConsoleAsync("Cannot set affinity provided cores. either one doesn't exist or the core id is invalid. " + coreIds);
                return;
            }
            ProcessAffinity.TryGetCpuMask(nameof(QCSettingsControl), coreIds, out long targetAffinity, out string _);
            ProcessAffinity.Setup(0, targetAffinity, out string result);
            QuantumConsole.Instance.LogToConsoleAsync("QCSettingsControl: " + result);
        }
#endif
        
        [Command("settings.view-distance", "Changes the view distance multiplier")][UnityEngine.Scripting.Preserve]
        static void MultiplyViewDistance(float multiplier) {
            World.Only<DistanceCullingSetting>().SetDebugValue(multiplier);
        }
    }
}