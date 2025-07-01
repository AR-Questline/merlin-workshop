using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;

namespace Awaken.TG.Main.Fights.Duels {
    [Serializable]
    public struct DuelArenaData {
        public SceneReference sceneRef;
        public LocationReference arenaRef;
    }
}
