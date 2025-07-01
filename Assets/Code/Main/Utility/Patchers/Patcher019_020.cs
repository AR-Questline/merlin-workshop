using System;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher019_020 : Patcher_DeleteSaves {
        protected override Version MaxInputVersion => new Version(0, 19);
        protected override Version FinalVersion => new Version(0, 20);
    }
}