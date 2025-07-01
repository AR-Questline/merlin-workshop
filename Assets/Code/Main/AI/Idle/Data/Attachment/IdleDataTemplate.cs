using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    public class IdleDataTemplate : Template {
        [SerializeField] IdleData data;

        public ref readonly IdleData Data => ref data;
        public float PositionRange => data.positionRange;
    }
}