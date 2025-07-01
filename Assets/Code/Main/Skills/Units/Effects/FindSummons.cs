using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Skills.Units.Listeners;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindSummons : ARLoopUnit {
        InlineValueInput<bool> _includeHostileToHero;
        InlineValueInput<bool> _includeFriendlyToHero;
        InlineValueInput<bool> _includeNeutralToHero;
        
        protected override IEnumerable Collection(Flow flow) {
            List<NpcElement> summons = World.All<NpcElement>().Where(npc => {
                if (!npc.IsSummon) {
                    return false;
                }
                Hero hero = Hero.Current;
                return (_includeHostileToHero.Value(flow) && npc.AntagonismTo(hero) == Antagonism.Hostile) ||
                       (_includeFriendlyToHero.Value(flow) && npc.AntagonismTo(hero) == Antagonism.Friendly) ||
                       (_includeNeutralToHero.Value(flow) && npc.AntagonismTo(hero) == Antagonism.Neutral);
            }).ToList();
            return summons;
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(NpcElement), "NpcElement");

        protected override void Definition() {
            _includeHostileToHero = InlineARValueInput("Hostile to hero", true);
            _includeFriendlyToHero = InlineARValueInput("Friendly to hero", true);
            _includeNeutralToHero = InlineARValueInput("Neutral to hero", true);
            base.Definition();
        }
    }
}