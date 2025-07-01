using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Registers in memory that Hero visited Place.
    /// </summary>
    [Element("Game/Journal: Unlock")]
    public class SEditorJournalUnlock : EditorStep {
        [GuidSelection]
        public JournalGuid guid;
        public EntryType entryType;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SJournalUnlock {
                guid = guid,
                entryType = entryType
            };
        }
    }

    public partial class SJournalUnlock : StoryStep {
        public JournalGuid guid;
        public EntryType entryType;
        public override StepResult Execute(Story story) {
            if (guid != default) {
                World.Only<PlayerJournal>().UnlockEntry(guid.GUID, GetJournalSubTabType());
            }
            return StepResult.Immediate;
        }

        JournalSubTabType GetJournalSubTabType() {
            return entryType switch {
                EntryType.Bestiary => JournalSubTabType.Bestiary,
                EntryType.Characters => JournalSubTabType.Characters,
                EntryType.Lore => JournalSubTabType.Lore,
                EntryType.Recipes => JournalSubTabType.Recipes,
                EntryType.Tutorials => JournalSubTabType.Tutorials,
                EntryType.Fish => JournalSubTabType.Fish,
            };
        }
    }

    [Serializable]
    public enum EntryType {
        Bestiary = 0,
        Characters = 1,
        Lore = 2,
        Recipes = 3,
        Tutorials = 4,
        Fish = 5
    }
}