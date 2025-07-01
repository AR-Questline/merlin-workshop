using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Registers in memory that Hero visited Place.
    /// </summary>
    [Element("Game/Journal: Check")]
    public class CEditorJournalCheck : EditorCondition {
        [GuidSelection]
        public JournalGuid guid;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CJournalCheck {
                guid = guid
            };
        }
    }

    public partial class CJournalCheck : StoryCondition {
        public JournalGuid guid;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return guid != default && World.Only<PlayerJournal>().WasEntryUnlocked(guid.GUID);
        }
    }
}