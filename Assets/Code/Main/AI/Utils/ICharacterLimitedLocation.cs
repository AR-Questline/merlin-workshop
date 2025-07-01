using Awaken.TG.Main.Character;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Utils {
    public enum CharacterLimitedLocationType : byte {
        [UnityEngine.Scripting.Preserve] None,
        NpcSummon,
        HeroSummon,
        PlacedMine
    }
    
    public interface ICharacterLimitedLocation : IModel {
        ICharacter Owner { get; }
        CharacterLimitedLocationType Type { get; }
        int LimitForCharacter(ICharacter character);
        void Destroy();
        void OwnerDiscarded() { 
            Destroy();
        }
    }
}