using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.AI.Graphs.Fight {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class PositionOf : ARUnit {
        const float MaxTargetingDelta = Mathf.PI / 6;

        [Serialize, Inspectable, UnitHeaderInspectable]
        public CharacterPoint point;

        ARValueInput<ICharacter> _character;

        protected override void Definition() {
            _character = RequiredARValueInput<ICharacter>("character");

            if (point == CharacterPoint.TargetingPoint) {
                var targetingMask = InlineARValueInput("mask", (LayerMask) 0);
                var targetingDistance = InlineARValueInput("distance", 25f);
                PositionOutput((flow, character) => GetTargetingPoint(character, targetingMask.Value(flow), targetingDistance.Value(flow)));
            } else {
                PositionOutput((flow, character) => {
                    return point switch {
                        CharacterPoint.MainHand => character.MainHand.position,
                        CharacterPoint.OffHand => character.OffHand.position,
                        CharacterPoint.Head => character.Head.position,
                        CharacterPoint.Torso => character.Torso.position,
                        CharacterPoint.TargetCharacter => character.GetCurrentTarget()?.Coords ?? Vector3.zero,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                });
            }
        }

        ValueOutput PositionOutput(Func<Flow, ICharacter, Vector3> position) {
            return ValueOutput("position", flow => {
                var character = _character.Value(flow);
                return position(flow, character);
            });
        }

        static Vector3 GetTargetingPoint(ICharacter character, LayerMask mask, float distance) {
            Vector3 origin;
            Vector3 direction;
            
            switch (character) {
                case Hero hero: 
                    var vHeroController = hero.VHeroController;
                    origin = vHeroController.CameraPosition;
                    direction = vHeroController.LookDirection;
                    break;
                
                case NpcElement npc:
                    origin = npc.Head.position;
                    var forward = npc.Torso.forward;
                    var directionToTarget = npc.GetCurrentTarget().Torso.position - origin;
                    direction = Vector3.RotateTowards(forward, directionToTarget.normalized, MaxTargetingDelta, 0.05f);
                    break;
                
                default:
                    throw new Exception($"Invalid character type: {character.GetType()}");
            }

            var ray = new Ray(origin, direction);
            if (Physics.Raycast(ray, out var hit, distance, mask)) {
                return hit.point;
            } else {
                return ray.GetPoint(distance);
            }
        }
    }

    public enum CharacterPoint {
        MainHand,
        OffHand,
        Head,
        Torso,
        TargetCharacter,
        TargetingPoint,
    }
}