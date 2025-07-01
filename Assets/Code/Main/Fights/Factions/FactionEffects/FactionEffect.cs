using System;
using Awaken.TG.Main.Localization;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.FactionEffects {
    [Serializable]
    public class FactionEffect {
        public ReputationKind reputationKind;
        public LocString effectDescription;
        [SerializeReference] [UnityEngine.Scripting.Preserve] public ReputationEffect[] reputationEffects;
    }
}