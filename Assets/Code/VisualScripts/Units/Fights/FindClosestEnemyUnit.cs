using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindClosestEnemyUnit : ARUnit {
        protected override void Definition() {
            var inCharacter = FallbackARValueInput("character", flow => flow.stack.self);
            var inCenter = OptionalARValueInput<Vector3>("center");
            var inRange = OptionalARValueInput<float>("range");
            
            ValueOutput("enemy", flow => {
                var character = VGUtils.GetModel<ICharacter>(inCharacter.Value(flow));
                var center = inCenter.Value(flow, () => character.Coords);

                ICharacter enemy = null;
                var minEnemyDistance = float.MaxValue;
                foreach (var findEnemy in character.FindEnemies()) {
                    var distance = (findEnemy.Coords - center).sqrMagnitude;
                    if (distance < minEnemyDistance) {
                        minEnemyDistance = distance;
                        enemy = findEnemy;
                    }
                }

                if (enemy != null && inRange.HasValidConnection) {
                    float range = inRange.Value(flow);
                    if ((enemy.Coords - center).sqrMagnitude > range * range) {
                        enemy = null;
                    }
                }
                return enemy?.CharacterView?.transform;
            });
        }
    }
}