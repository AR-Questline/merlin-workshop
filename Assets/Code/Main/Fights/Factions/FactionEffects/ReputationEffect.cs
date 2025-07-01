using System;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Fights.Factions.FactionEffects {

    [Serializable]
    public abstract class ReputationEffect {
        protected FactionTemplate _factionTemplate;
        [UnityEngine.Scripting.Preserve] public FactionTemplate FactionTemplate => _factionTemplate;
        
        public abstract void Init(FactionTemplate factionTemplate);

        public abstract void Deinit();
    }
}