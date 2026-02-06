using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.SummonsAndAllies {
    public partial class NpcHeroPetAlly : NpcHeroSummon {
        public override ushort TypeForSerialization => SavedModels.NpcHeroPetAlly;
        
        public override bool DestroyOnRest => false;
        public override CharacterLimitedLocationType Type => CharacterLimitedLocationType.None;
        public override int LimitForCharacter(ICharacter character) => 1;

        public NpcHeroPetAlly(Hero owner) : base(owner, null, 0) { }
    }
}