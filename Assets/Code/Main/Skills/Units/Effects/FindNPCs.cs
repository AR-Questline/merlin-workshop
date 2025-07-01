using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
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
    [UnitTitle("Find NPCs")]
    public class FindNPCs : ARLoopUnit, ISkillUnit {
        [UnityEngine.Scripting.Preserve] static readonly NpcElement[] EmptyNpcCollection = Array.Empty<NpcElement>();
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public NpcSearchType SearchType { get; set; } = NpcSearchType.Sphere;
        
        ARValueInput<Vector3> _origin;
        ARValueInput<Vector3> _direction;
        ARValueInput<float> _range;
        ARValueInput<float> _angle;
        ARValueInput<bool> _antagonistic;
        ARValueInput<bool> _neutral;
        ARValueInput<bool> _ally;
        ARValueInput<ICharacter> _owner;

        protected override IEnumerable Collection(Flow flow) {
            var owner = _owner.Value(flow);
            var range = _range.Value(flow);
            var antagonistic = _antagonistic.Value(flow);
            var neutral = _neutral.Value(flow);
            var ally = _ally.Value(flow);
            var npcGrid = World.Services.Get<NpcGrid>();
            var angleAsRadians = (_angle?.Value(flow) * Mathf.Deg2Rad) ?? 0;
            
            switch (SearchType) {
                case NpcSearchType.Sphere: {
                    var npcs = npcGrid.GetNpcsInSphere(_origin.Value(flow), range);
                    return FindNpcUtil.FindFromCollection(npcs, owner, antagonistic, neutral, ally);
                }
                case NpcSearchType.Cone: {
                    var npcs = npcGrid.GetNpcsInCone(_origin.Value(flow), _direction.Value(flow), range, angleAsRadians);
                    return FindNpcUtil.FindFromCollection(npcs, owner, antagonistic, neutral, ally);
                }
                case NpcSearchType.ClosestToCrosshair: {
                    var npcs = npcGrid.GetNpcsInCone(owner.Coords, owner.ParentTransform.forward, range, angleAsRadians);
                    return FindNpcUtil.FindClosestToCrosshair(npcs, owner, range, antagonistic, neutral, ally);
                }
                default:
                    return null;
            }
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(NpcElement), "NpcElement");

        protected override void Definition() {
            _owner = FallbackARValueInput("character", flow => this.Skill(flow).Owner);

            if (SearchType != NpcSearchType.ClosestToCrosshair) {
                _origin = FallbackARValueInput("origin", flow => _owner.Value(flow).Coords);
            }

            if (SearchType == NpcSearchType.Cone) {
                _direction = FallbackARValueInput("direction", flow => _owner.Value(flow).ParentTransform.forward);
            }

            if (SearchType != NpcSearchType.Sphere) {
                _angle = FallbackARValueInput("angle", _ => 30f);
            }

            _range = FallbackARValueInput("range", _ => 10f);
            _antagonistic = FallbackARValueInput("antagonistic", _ => true);
            _neutral = FallbackARValueInput("neutral", _ => true);
            _ally = FallbackARValueInput("ally", _ => true);
            
            base.Definition();
        }
        
        public enum NpcSearchType : byte {
            Cone = 0,
            Sphere = 1,
            ClosestToCrosshair = 2
        }
    }
}