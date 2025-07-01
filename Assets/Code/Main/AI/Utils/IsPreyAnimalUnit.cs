using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Utils {
    [UnitCategory("AR/AI_Systems/Utils")]
    [TypeIcon(typeof(Variables))]
    [UnityEngine.Scripting.Preserve]
    public class IsPreyAnimalUnit : ARUnit {
        protected override void Definition() {
            var characterInput = FallbackARValueInput("character", flow => flow.stack.self);
            ValueOutput("isPreyAnimal", flow => {
                ICharacter character = VGUtils.GetModel<ICharacter>(characterInput.Value(flow));
                if (character != null) {
                    var faction = character.Faction;
                    var factionService = World.Services.Get<FactionService>();
                    return faction.Is(factionService.DomesticAnimals) || faction.Is(factionService.PreyAnimals);
                } else {
                    return false;
                }
            });
        }
    }
}

