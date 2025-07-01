using System;

namespace Awaken.TG.Main.Stories.Quests.UI {
    public enum QuestType : byte {
        [UnityEngine.Scripting.Preserve] Main = 0,
        [UnityEngine.Scripting.Preserve] Side = 1,
        [UnityEngine.Scripting.Preserve] Challenge = 2,
        [UnityEngine.Scripting.Preserve] Achievement = 3,
        [UnityEngine.Scripting.Preserve] Misc = 4,
    };
    
    [Flags]
    public enum QuestListType {
        [UnityEngine.Scripting.Preserve] None = 0,
        Main = 1 << 1,
        Side = 1 << 2,
        Completed = 1 << 3,
        Misc = 1 << 4,
        Failed = 1 << 5,
        
        [UnityEngine.Scripting.Preserve] All = Main | Side | Misc,
    };
}
