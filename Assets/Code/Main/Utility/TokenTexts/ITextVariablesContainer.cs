using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.Utility.TokenTexts {
    public interface ITextVariablesContainer {
        float? GetVariable(string id, int index = 0, ICharacter owner = null);
        StatType GetEnum(string id, int index = 0);
    }
}