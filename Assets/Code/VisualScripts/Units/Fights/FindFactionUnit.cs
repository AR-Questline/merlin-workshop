using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindFactionUnit : FindCharactersUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public Faction faction;
        protected override string CharacterAlias => "faction";
        protected override IEnumerable<ICharacter> FindCharacters(ICharacter character) {
            return AIUtils.ValidCharacters().Where(c => c.Faction == faction);
        }
    }
}