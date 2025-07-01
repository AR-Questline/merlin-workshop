using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Skills.Units.Listeners;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class FindDeadBodies : ARLoopUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public NpcSearchType SearchType { get; set; } = NpcSearchType.Sphere;
        
        ARValueInput<Vector3> _origin;
        ARValueInput<Vector3> _direction;
        ARValueInput<float> _range;
        ARValueInput<float> _angle;
        ARValueInput<ICharacter> _owner;

        protected override IEnumerable Collection(Flow flow) {
            var owner = _owner.Value(flow);
            var range = _range.Value(flow);
            var npcGrid = World.Services.Get<NpcGrid>();
            var angleAsRadians = (_angle?.Value(flow) * Mathf.Deg2Rad) ?? 0;
            
            switch (SearchType) {
                case NpcSearchType.Sphere: {
                    return npcGrid.GetNpcDummiesInSphere(_origin.Value(flow), range);
                }
                case NpcSearchType.Cone: {
                    return npcGrid.GetNpcDummiesInCone(_origin.Value(flow), _direction.Value(flow), range, angleAsRadians);
                }
                case NpcSearchType.ClosestToCrosshair: {
                    var npcs = npcGrid.GetNpcDummiesInCone(owner.Coords, owner.ParentTransform.forward, range, angleAsRadians);
                    return FindClosestToCrosshair(npcs, owner, range);
                }
                default:
                    return null;
            }
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(NpcDummy), "NpcDummy");

        protected override void Definition() {
            _range = InlineARValueInput("range", 10f);
            _owner = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            
            if (SearchType != NpcSearchType.ClosestToCrosshair) {
                _origin = FallbackARValueInput("origin", flow => _owner.Value(flow).Coords);
            }
            
            if (SearchType == NpcSearchType.Cone) {
                _direction = FallbackARValueInput("direction", flow => _owner.Value(flow).ParentTransform.forward);
            }
            
            if (SearchType != NpcSearchType.Sphere) {
                _angle = InlineARValueInput("angle", 30f);
            }

            base.Definition();
        }
        
        static IEnumerable<NpcDummy> FindClosestToCrosshair(IEnumerable<NpcDummy> npcs, ICharacter owner, float range) {
            var firePoint = Hero.Current.VHeroController.FirePoint;
            var origin = firePoint.position;
            var direction = firePoint.forward;
            Ray ray = new(origin, direction);
            
            if (Physics.Raycast(ray, out RaycastHit hit, range, RenderLayers.Mask.AIs)) {
                if (VGUtils.TryGetModel<NpcDummy>(hit.collider.gameObject, out var dummy)) {
                    yield return dummy;
                }
            }

            foreach (var dummy in ClosestDummies(npcs, ray)) {
                yield return dummy;
            }
        }

        static IEnumerable<NpcDummy> ClosestDummies(IEnumerable<NpcDummy> npcs, Ray ray) {
            return npcs.OrderByDescending(npc => Vector3.Dot(ray.direction, npc.Coords - ray.origin));
        }
        
        public enum NpcSearchType : byte {
            Cone = 0,
            Sphere = 1,
            ClosestToCrosshair = 2
        }
    }
}