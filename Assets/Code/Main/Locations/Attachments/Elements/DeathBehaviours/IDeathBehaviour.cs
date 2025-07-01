using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public interface IDeathBehaviour {
        bool IsVisualInitialized => false;
        bool BlockExternalCustomDeath => false;
        void OnVisualLoaded(DeathElement death, Transform transform);
        void OnDeath(DamageOutcome damageOutcome, Location dyingLocation);
        bool UseDeathAnimation { get; }
        NpcDeath.DeathAnimType UseCustomDeathAnimation { get; }
    }
}