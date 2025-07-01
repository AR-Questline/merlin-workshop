using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.NPCs {
    
    [UnitCategory("AR/NPCs/Factions")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OverrideFaction : ARUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            var faction = RequiredARValueInput<TemplateWrapper<FactionTemplate>>("faction");
            DefineNoNameAction(flow => character.Value(flow).OverrideFaction(faction.Value(flow).Template));
        }
    }

    [UnitCategory("AR/NPCs/Factions")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ResetFactionOverride : ARUnit {
        protected override void Definition() {
            var location = RequiredARValueInput<ICharacter>("character");
            DefineNoNameAction(flow => location.Value(flow).ResetFactionOverride());
        }
    }

    [UnitCategory("AR/NPCs/Factions")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetAntagonism : ARUnit {
        protected override void Definition() {
            var from = RequiredARValueInput<ICharacter>("from");
            var to = RequiredARValueInput<ICharacter>("to");
            
            ValueOutput("antagonism", flow => from.Value(flow).AntagonismTo(to.Value(flow)));
            
            ValueOutput("isFriendly", flow => from.Value(flow).IsFriendlyTo(to.Value(flow)));
            ValueOutput("isNeutral", flow => from.Value(flow).IsNeutralTo(to.Value(flow)));
            ValueOutput("isHostile", flow => from.Value(flow).IsHostileTo(to.Value(flow)));
        }
    }
    
    [UnitCategory("AR/NPCs/Factions")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SwitchOnAntagonism : ARUnit {
        protected override void Definition() {
            var from = RequiredARValueInput<ICharacter>("from");
            var to = RequiredARValueInput<ICharacter>("to");
            
            var friendly = ControlOutput("friendly");
            var neutral = ControlOutput("neutral");
            var hostile = ControlOutput("hostile");
            
            ControlInput("", flow => {
                return from.Value(flow).AntagonismTo(to.Value(flow)) switch {
                    Antagonism.Friendly => friendly,
                    Antagonism.Neutral => neutral,
                    Antagonism.Hostile => hostile,
                    _ => throw new ArgumentOutOfRangeException()
                };
            });
        }
    }
}