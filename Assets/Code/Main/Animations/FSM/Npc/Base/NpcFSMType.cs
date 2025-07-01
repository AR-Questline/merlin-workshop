namespace Awaken.TG.Main.Animations.FSM.Npc.Base {
    public enum NpcFSMType : byte {
        GeneralFSM = 0,
        AdditiveFSM = 1,
        CustomActionsFSM = 2,
        TopBodyFSM = 3,
        OverridesFSM = 4,
        [UnityEngine.Scripting.Preserve] LegsFSM = 5,
    }
}