using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindMyFactionUnit : FindCharactersUnit {
        protected override string CharacterAlias => "myFaction";
        protected override IEnumerable<ICharacter> FindCharacters(ICharacter character) {
            return AIUtils.ValidCharacters().Where(c => c.Faction == character.Faction);
        }
    }
}