using System;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [Serializable]
    public struct InteractionCustomData {
        public string name;
        [HideLabel, InlineProperty] public InteractionData action;
    }
}