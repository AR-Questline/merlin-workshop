using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Skills.Units.Listeners;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    public class FindCharacters : ARLoopUnit {
        [UnityEngine.Scripting.Preserve] const int DefaultMask = RenderLayers.Mask.Hitboxes | RenderLayers.Mask.Player;
        
        FallbackValueInput<Vector3> _origin;
        FallbackValueInput<float> _range;
        
        protected override IEnumerable Collection(Flow flow) {
            return Find(_origin.Value(flow), _range.Value(flow));
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(ICharacter), "CharacterInterface");

        protected override void Definition() {
            _origin = FallbackARValueInput("position", _ => Vector3.zero);
            _range = FallbackARValueInput("range", _ => 10f);
            base.Definition();
        }
        
        IEnumerable<ICharacter> Find(Vector3 origin, float range) {
            foreach (var npc in World.Services.Get<NpcGrid>().GetNpcsInSphere(origin, range)) {
                yield return npc;
            }
            if (Hero.Current is { } hero) {
                if (hero.Coords.SquaredDistanceTo(origin) < math.square(range)) {
                    yield return hero;
                }
            }
        }
    }
}