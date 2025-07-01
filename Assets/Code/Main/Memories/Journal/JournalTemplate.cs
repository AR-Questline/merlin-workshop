using System.Collections.Generic;
using Awaken.TG.Main.Memories.Journal.Entries;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Memories.Journal {
    public class JournalTemplate : Template {
        public IEnumerable<EntryData> GetEntryDatas() {
            return GetComponentsInChildren<JournalEntryTemplateData>().SelectWithLog(e => e.GenericData);
        }
    }
}
