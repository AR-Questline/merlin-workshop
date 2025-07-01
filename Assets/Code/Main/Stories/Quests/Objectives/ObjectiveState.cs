using System;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    public enum ObjectiveState {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Failed = 3,
    }

    [Flags]
    public enum ObjectiveStateFlag {
        [UnityEngine.Scripting.Preserve] None = 0,
        [UnityEngine.Scripting.Preserve] Inactive = 1 << ObjectiveState.Inactive,
        [UnityEngine.Scripting.Preserve] Active = 1 << ObjectiveState.Active,
        [UnityEngine.Scripting.Preserve] Completed = 1 << ObjectiveState.Completed,
        [UnityEngine.Scripting.Preserve] Failed = 1 << ObjectiveState.Failed,
        [UnityEngine.Scripting.Preserve] NotFinished = Inactive | Active,
        [UnityEngine.Scripting.Preserve] Finished = Completed | Failed,
        [UnityEngine.Scripting.Preserve] All = Inactive | Active | Completed | Failed,
    }
}