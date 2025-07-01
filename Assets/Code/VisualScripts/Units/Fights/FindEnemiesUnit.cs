using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Fights;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindEnemiesUnit : FindCharactersUnit {
        protected override string CharacterAlias => "enemies";
        protected override IEnumerable<ICharacter> FindCharacters(ICharacter character) {
            return character.FindEnemies();
        }
    }
}
