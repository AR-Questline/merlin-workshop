using System.Linq;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Memories.Journal.Entries;
using Awaken.TG.Main.Memories.Journal.Entries.Implementations;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetJournalCountUnit : ARUnit {
        protected override void Definition() {
            ValueOutput("enemiesDiscoveredCount", GetEnemiesDiscoveredCount);
            ValueOutput("characterDiscoveredCount", GetCharacterDiscoveredCount);
            ValueOutput("loreDiscoveredCount", GetLoreDiscoveredCount);
        }
        
        static int GetEnemiesDiscoveredCount(Flow flow) {
            return World.Only<PlayerJournal>().GetEntries<BeastiaryRuntime.BeastiaryData>().Count(IsCompleted);
        }
        
        static int GetCharacterDiscoveredCount(Flow flow) {
            return World.Only<PlayerJournal>().GetEntries<CharacterRuntime.CharacterData>().Count(IsCompleted);
        }

        static int GetLoreDiscoveredCount(Flow flow) {
            return World.Only<PlayerJournal>().GetEntries<LoreEntryRuntime.LoreJournalData>().Count(IsCompleted);
        }

        static bool IsCompleted(EntryData data) {
            return data?.conditionForEntry?.IsMet() ?? true;
        }
    }
}