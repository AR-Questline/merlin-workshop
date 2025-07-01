using Awaken.TG.Main.Memories.Journal.Conditions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    [HideMonoScript]
    public abstract class JournalEntryTemplateData : MonoBehaviour {
        [SerializeField] 
        JournalGuid guid;

        [UnityEngine.Scripting.Preserve] public JournalGuid Guid => guid;
        public abstract EntryData GenericData { get; }
        
#if UNITY_EDITOR
        [Button("Toggle Guids Visible"), PropertyOrder(-1), HorizontalGroup("TopButtons")]
        static void EDITOR_ToggleGuidsVisible() {
            JournalGuid.EDITOR_GuidsVisible = !JournalGuid.EDITOR_GuidsVisible;
        }
#endif
    }
}