using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.Main.Character {
    public static class HandOwnerUtils {
        public static IHandOwner<ICharacter> GetHandOwner(this ICharacter character) {
            return character switch {
                Hero hero => hero.TryGetElement<HeroHandOwner>(),
                NpcElement npc => npc.TryGetElement<NpcHandOwner>(),
                _ => null
            };
        }
    }
}