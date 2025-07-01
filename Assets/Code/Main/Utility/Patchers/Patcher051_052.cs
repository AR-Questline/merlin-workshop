using System;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher051_052 : Patcher_DeleteSaves {
        protected override Version MaxInputVersion => new Version(0, 51, 9999);
        protected override Version FinalVersion => new Version(0, 52);
    }
}