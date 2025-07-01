using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    [InfoBox("Do not create more than one of this entry", InfoMessageType.Warning)]
    public abstract class SingletonJournalEntryData : JournalEntryTemplateData { }
}