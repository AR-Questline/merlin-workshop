using System;

namespace Awaken.TG.Utility.Attributes.List {
    [Flags]
    public enum ListEditOption {
        [UnityEngine.Scripting.Preserve] None = 0,
        ListSize = 1,
        ListLabel = 2,
        ElementLabels = 4,
        Buttons = 8,
        FewButtons = 16,
        NullNewElement = 32,
        [UnityEngine.Scripting.Preserve] Default = ListSize | ListLabel | ElementLabels,
        [UnityEngine.Scripting.Preserve] NoElementLabels = ListSize | ListLabel,
        [UnityEngine.Scripting.Preserve] All = Default | Buttons
    }
}
