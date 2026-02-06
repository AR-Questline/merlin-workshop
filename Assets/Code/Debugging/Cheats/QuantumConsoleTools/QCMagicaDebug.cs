using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Heroes;
using MagicaCloth2;
using QFSW.QC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCMagicaDebug {
        [Command("hero.reset-cloth", "Resets all MagicaCloths on the current hero")]
        [UnityEngine.Scripting.Preserve]
        static void ResetHeroCloth(bool keepPose = false) {
            if (Hero.Current == null) {
                QuantumConsole.Instance.LogToConsoleAsync("No hero found");
                return;
            }

            Transform parentTransform = Hero.Current.ParentTransform;
            if (parentTransform == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Hero has no ParentTransform");
                return;
            }

            MagicaCloth[] cloths = parentTransform.GetComponentsInChildren<MagicaCloth>();
            if (cloths.Length == 0) {
                QuantumConsole.Instance.LogToConsoleAsync("No MagicaCloths found on hero");
                return;
            }

            foreach (MagicaCloth cloth in cloths) {
                cloth.ResetCloth(keepPose);
            }

            QuantumConsole.Instance.LogToConsoleAsync($"Reset {cloths.Length} MagicaCloth(s) on hero (keepPose: {keepPose})");
        }

        [Command("hero.set-cloth-flag", "Sets a TeamManager flag on all MagicaCloths on the current hero")]
        [UnityEngine.Scripting.Preserve]
        static void SetClothFlag([TeamManagerFlag] string flagName, bool value) {
            if (Hero.Current == null) {
                QuantumConsole.Instance.LogToConsoleAsync("No hero found");
                return;
            }

            Transform parentTransform = Hero.Current.ParentTransform;
            if (parentTransform == null) {
                QuantumConsole.Instance.LogToConsoleAsync("Hero has no ParentTransform");
                return;
            }

            MagicaCloth[] cloths = parentTransform.GetComponentsInChildren<MagicaCloth>();
            if (cloths.Length == 0) {
                QuantumConsole.Instance.LogToConsoleAsync("No MagicaCloths found on hero");
                return;
            }

            int flagIndex = GetFlagIndex(flagName);
            if (flagIndex == -1) {
                QuantumConsole.Instance.LogToConsoleAsync($"Unknown flag: {flagName}");
                return;
            }

            int affectedCount = 0;
            foreach (MagicaCloth cloth in cloths) {
                if (cloth.IsValid()) {
                    ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cloth.Process.TeamId);
                    tdata.flag.SetBits(flagIndex, value);
                    affectedCount++;
                }
            }

            QuantumConsole.Instance.LogToConsoleAsync($"Set flag '{flagName}' to {value} on {affectedCount} MagicaCloth(s)");
        }

        static int GetFlagIndex(string flagName) {
            return flagName switch {
                "Valid" => TeamManager.Flag_Valid,
                "Enable" => TeamManager.Flag_Enable,
                "Reset" => TeamManager.Flag_Reset,
                "TimeReset" => TeamManager.Flag_TimeReset,
                "Suspend" => TeamManager.Flag_Suspend,
                "Running" => TeamManager.Flag_Running,
                "Synchronization" => TeamManager.Flag_Synchronization,
                "StepRunning" => TeamManager.Flag_StepRunning,
                "Exit" => TeamManager.Flag_Exit,
                "KeepTeleport" => TeamManager.Flag_KeepTeleport,
                "InertiaShift" => TeamManager.Flag_InertiaShift,
                "CullingInvisible" => TeamManager.Flag_CullingInvisible,
                "CullingKeep" => TeamManager.Flag_CullingKeep,
                "Spring" => TeamManager.Flag_Spring,
                "SkipWriting" => TeamManager.Flag_SkipWriting,
                "Anchor" => TeamManager.Flag_Anchor,
                "AnchorReset" => TeamManager.Flag_AnchorReset,
                "NegativeScale" => TeamManager.Flag_NegativeScale,
                _ => -1
            };
        }
    }
}