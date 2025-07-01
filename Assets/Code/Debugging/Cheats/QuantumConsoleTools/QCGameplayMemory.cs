using System.Linq;
using System.Text;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.MVC;
using QFSW.QC;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCGameplayMemory {
        [Command("memory.display-faction-data", "Displays all faction data stored in memory.")][UnityEngine.Scripting.Preserve]
        static void MemoryDisplayFactionData() {
            var entries = World.Services.Get<GameplayMemory>()
                .FilteredContextsBy(Faction.FactionContext)
                .Select(c => (c.Selector, c.GetAll()
                    .Where(f => !f.Key.Contains("once"))
                    .Select(f => new StoryDebugTool.MemoryEntry(f.Key, f.Value, c.Selector)).ToArray()));
            
            var sb = new StringBuilder();
            sb.AppendLine("Faction memory data:");
            foreach (var entry in entries) {
                if (entry.Item2.Length == 0) continue;
                sb.AppendLine("[" + entry.Selector + "]");
                
                foreach (var data in entry.Item2) {
                    sb.AppendLine($"-- {data.key} -> {data.value}");
                }
            }
            string msg = sb.ToString();
            
            QuantumConsole.Instance.LogToConsoleAsync(msg);
        }
        
        [Command("memory.set-faction-bounty", "Set the reputation of a faction.")][UnityEngine.Scripting.Preserve]
        static void SetFactionBounty([TemplateSuggestion(typeof(CrimeOwnerTemplate))] CrimeOwnerTemplate faction, float value) {
            if (faction == null) {
                QuantumConsole.Instance.LogToConsoleAsync($"Faction {faction} not found.");
                return;
            }
            
            CrimeUtils.ClearBounty(faction);
            CrimeUtils.AddBounty(faction, value, out _);
            QuantumConsole.Instance.LogToConsoleAsync($"Set bounty for {faction.name} to {value}.");
        }
        
        [Command("memory.clear-faction-bounty", "Clear the bounty of a faction.")][UnityEngine.Scripting.Preserve]
        static void ClearBounty([TemplateSuggestion(typeof(CrimeOwnerTemplate))] CrimeOwnerTemplate faction) {
            if (faction == null) {
                Log.Important?.Error($"Faction {faction} not found.");
                return;
            }
            
            CrimeUtils.ClearBounty(faction);
            QuantumConsole.Instance.LogToConsoleAsync($"Cleared bounty for {faction.name}.");
        }
        
        [Command("memory.clear-faction-unforgivable-crime", "Clear unforgivable crime committed against a faction.")][UnityEngine.Scripting.Preserve]
        static void ClearUnforgivableCrime([TemplateSuggestion(typeof(CrimeOwnerTemplate))] CrimeOwnerTemplate faction) {
            if (faction == null) {
                QuantumConsole.Instance.LogToConsoleAsync($"Faction {faction} not found.");
                return;
            }
            
            CrimeUtils.ClearUnforgivableCrime(faction);
            QuantumConsole.Instance.LogToConsoleAsync($"Cleared unforgivable crime for {faction.name}.");
        }
        
        [Command("memory.unlock-skip-prologue", "Unlocks Skip Prologue in the title screen")][UnityEngine.Scripting.Preserve]
        static void UnlockSkipPrologue(bool unlock = true) {
            PrefMemory.Set(TitleScreenUI.SkipPrologueUnlockKey, unlock, true);
            string log = $"{(unlock ? "Unlocked" : "Locked")} skip prologue. New Game button in the title screen should be updated.";
            QuantumConsole.Instance.LogToConsoleAsync(log);
        }
    }
}
