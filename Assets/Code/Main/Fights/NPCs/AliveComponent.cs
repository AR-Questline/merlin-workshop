using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class AliveComponent : MonoBehaviour {
        public SerializedArrayDictionary<EquatableCollider, HitboxData> hitboxes = SerializedArrayDictionary<EquatableCollider, HitboxData>.Empty;

    }
}