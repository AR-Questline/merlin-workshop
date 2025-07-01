using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Skills.Units.Listeners;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    public class FindAlives : ARLoopUnit, ISkillUnit {
        [UnityEngine.Scripting.Preserve] static readonly IAlive[] EmptyNpcCollection = Array.Empty<IAlive>();
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public NpcSearchType searchType = NpcSearchType.Sphere;
        
        ARValueInput<Vector3> _origin;
        ARValueInput<Vector3> _direction;
        ARValueInput<float> _range;
        ARValueInput<float> _angle;
        ARValueInput<bool> _hostile;
        ARValueInput<bool> _neutral;
        ARValueInput<bool> _ally;
        ARValueInput<ICharacter> _owner;

        protected override IEnumerable Collection(Flow flow) {
            var owner = _owner.Value(flow);
            var range = _range.Value(flow);
            var hostile = _hostile.Value(flow);
            var neutral = _neutral.Value(flow);
            var ally = _ally.Value(flow);
            var npcGrid = World.Services.Get<NpcGrid>();
            var angleAsRadians = (_angle?.Value(flow) * Mathf.Deg2Rad) ?? 0;
            
            switch (searchType) {
                case NpcSearchType.Sphere: {
                    var center = _origin.Value(flow);
                    var npcs = npcGrid.GetNpcsInSphere(center, range);
                    var alives = npcGrid.GetAliveLocationsInSphere(center, range);
                    return FindFromCollection(npcs, alives, owner, hostile, neutral, ally);
                }
                case NpcSearchType.Cone: {
                    var center = _origin.Value(flow);
                    var direction = _direction.Value(flow);
                    math.sincos(angleAsRadians, out var sin, out var cos);
                    var npcs = npcGrid.GetNpcsInCone(center, direction, range, cos, sin);
                    var alives = npcGrid.GetAliveLocationsInCone(center, direction, range, cos, sin);
                    return FindFromCollection(npcs, alives, owner, hostile, neutral, ally);
                }
                case NpcSearchType.ClosestToCrosshair: {
                    math.sincos(angleAsRadians, out var sin, out var cos);
                    var npcs = npcGrid.GetNpcsInCone(owner.Coords, owner.ParentTransform.forward, range, cos, sin);
                    var alives = npcGrid.GetAliveLocationsInCone(owner.Coords, owner.ParentTransform.forward, range, cos, sin);
                    return FindClosestToCrosshair(npcs, alives, owner, range, hostile, neutral, ally);
                }
                default:
                    return null;
            }
        }

        protected override ValueOutput Payload() => ValueOutput(typeof(IAlive), "Alive");

        protected override void Definition() {
            _owner = FallbackARValueInput("character", flow => this.Skill(flow).Owner);

            if (searchType != NpcSearchType.ClosestToCrosshair) {
                _origin = FallbackARValueInput("origin", flow => _owner.Value(flow).Coords);
            }

            if (searchType == NpcSearchType.Cone) {
                _direction = FallbackARValueInput("direction", flow => _owner.Value(flow).ParentTransform.forward);
            }

            if (searchType != NpcSearchType.Sphere) {
                _angle = InlineARValueInput("angle", 30f);
            }

            _range = InlineARValueInput("range", 10f);
            _hostile = InlineARValueInput("hostile", true);
            _neutral = InlineARValueInput("neutral", true);
            _ally = InlineARValueInput("ally", true);
            
            base.Definition();
        }

        static IEnumerable<IAlive> FindFromCollection(IEnumerable<NpcElement> npcs, IEnumerable<AliveLocation> alives, ICharacter owner, bool hostile, bool neutral, bool ally) {
            var antagonismTo = owner.TryGetElement(out INpcSummon summon) ? summon.Owner : owner;
            foreach (var npc in npcs) {
                if (IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), hostile, neutral, ally)) {
                    yield return npc;
                }
            }
            foreach (var alive in alives) {
                yield return alive;
            }
        }
        
        static IEnumerable<IAlive> FindClosestToCrosshair(IEnumerable<NpcElement> npcs, IEnumerable<AliveLocation> alives, ICharacter owner, float range, bool hostile, bool neutral, bool ally) {
            var antagonismTo = owner.TryGetElement(out INpcSummon summon) ? summon.Owner : owner;

            var firePoint = Hero.Current.VHeroController.FirePoint;
            var origin = firePoint.position;
            var direction = firePoint.forward;
            var ray = new Ray(origin, direction);
            
            if (Physics.Raycast(ray, out RaycastHit hit, range, RenderLayers.Mask.AIs)) {
                switch (VGUtils.GetModel<IAlive>(hit.collider.gameObject)) {
                    case NpcElement npc:
                        if (IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), hostile, neutral, ally)) {
                            yield return npc;
                        }
                        break;
                    case AliveLocation alive:
                        yield return alive;
                        break;
                }
            }

            foreach (var alive in ClosestAlive(npcs, alives, hostile, neutral, ally, ray, antagonismTo)) {
                yield return alive;
            }
        }

        static IEnumerable<IAlive> ClosestAlive(IEnumerable<NpcElement> npcs, IEnumerable<AliveLocation> alives, bool hostile, bool neutral, bool ally, Ray ray, ICharacter antagonismTo) {
            List<IAlive> matchingAlives = new(alives);
            matchingAlives.AddRange(npcs.Where(npc => IsMatchingAntagonism(npc.AntagonismTo(antagonismTo), hostile, neutral, ally)));
            return matchingAlives.OrderByDescending(npc => Vector3.Dot(ray.direction, npc.Coords - ray.origin));
        }

        static bool IsMatchingAntagonism(Antagonism antagonism, bool hostile, bool neutral, bool ally) {
            return antagonism switch {
                Antagonism.Neutral => neutral,
                Antagonism.Friendly => ally,
                Antagonism.Hostile => hostile, 
                _ => false
            };
        }
        
        public enum NpcSearchType : byte {
            Cone = 0,
            Sphere = 1,
            ClosestToCrosshair = 2
        }
    }
}