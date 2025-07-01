using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Idle.Finders;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs.Presences {
    public enum NpcPresenceDetachReason : byte {
        ChangePresence,
        Death,
        MySceneUnloading,
    }
}