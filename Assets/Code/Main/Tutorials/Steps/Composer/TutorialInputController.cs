using System;

namespace Awaken.TG.Main.Tutorials.Steps.Composer {
    [Flags]
    public enum TutorialInputController {
        [UnityEngine.Scripting.Preserve] None = 0,
        MouseKeyboard = 1 << 0,
        Gamepad = 1 << 1,
        Any = MouseKeyboard | Gamepad,
    }
}