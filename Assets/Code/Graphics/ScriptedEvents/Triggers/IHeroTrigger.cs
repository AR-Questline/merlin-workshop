using System;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    public interface IHeroTrigger {
        public event Action OnHeroEnter;
        public event Action OnHeroExit;
    }
}