using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.AI.SummonsAndAllies;
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
            List<NpcElement> summons = new();
            bool includeHostile = _includeHostileToHero.Value(flow);
            bool includeFriendly = _includeFriendlyToHero.Value(flow);
            bool includeNeutral = _includeNeutralToHero.Value(flow);
            foreach (var summon in World.All<INpcSummon>()) {
                Hero hero = Hero.Current;
                NpcElement npc = summon.ParentModel;
                if ((includeHostile && npc.AntagonismTo(hero) == Antagonism.Hostile) ||
                    (includeFriendly && npc.AntagonismTo(hero) == Antagonism.Friendly) ||
                    (includeNeutral && npc.AntagonismTo(hero) == Antagonism.Neutral)) {
                    summons.Add(npc);
                }
            }
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