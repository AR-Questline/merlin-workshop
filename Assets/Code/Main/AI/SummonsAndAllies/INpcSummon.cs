using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.AI.SummonsAndAllies {
    public interface INpcSummon : IElement<NpcElement>, ICharacterLimitedLocation {
        public float ManaExpended { get; }
        public bool IsAlive { get; }
        
        public static class Events {
            public static readonly Event<ICharacter, INpcSummon> SummonSpawned = new(nameof(SummonSpawned));
        }
    }
}