using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Skills.Units.Listeners;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    public class GetNPCsInChain : ARLoopUnit {
        InlineValueInput<int> _chainCount;
        InlineValueInput<float> _chainRange;
        InlineValueInput<float> _chainRangeDecreasePerLink;
        RequiredValueInput<Vector3> _startCoords;
        InlineValueInput<Antagonism> _targetType;
        InlineValueInput<bool> _canTargetOwner;
        OptionalValueInput<ICharacter> _targetToIgnore;
        RequiredValueInput<ICharacter> _chainOwner;

        readonly List<ICharacter> _visited = new(8);

        protected override void Definition() {
            _chainCount = InlineARValueInput("chainCount", 3);
            _chainRange = InlineARValueInput("chainRange", 5f);
            _chainRangeDecreasePerLink = InlineARValueInput("chainRangeDecreasePerLink", 0f);

            _startCoords = RequiredARValueInput<Vector3>("startCoords");
            _targetToIgnore = OptionalARValueInput<ICharacter>("targetToIgnore");
            _chainOwner = RequiredARValueInput<ICharacter>("chainOwner");
            _canTargetOwner = InlineARValueInput("canTargetOwner", false);
            _targetType = InlineARValueInput("targetType", Antagonism.Hostile);
            base.Definition();
        }

        protected override IEnumerable Collection(Flow flow) {
            ICharacter owner = _chainOwner.Value(flow);
            
            Antagonism shouldTarget = _targetType.Value(flow);
            Vector3 searchCoords = _startCoords.Value(flow);
            float radius = _chainRange.Value(flow);
            int chainCount = _chainCount.Value(flow);
            
            bool canTargetOwner = _canTargetOwner.Value(flow);
            float chainRangeDecreasePerLink = Mathf.Clamp01(_chainRangeDecreasePerLink.Value(flow));
            
            
            List<NpcElement> result = new(chainCount);
            
            _visited.Clear();
            if (_targetToIgnore.HasValue) {
                _visited.Add(_targetToIgnore.Value(flow));
            }
            if (!canTargetOwner) {
                _visited.Add(owner);
            }
            
            var worldGrid = World.Services.Get<NpcGrid>();

            for (int i = chainCount - 1; i >= 0; i--) {
                var nearbyNPCs = worldGrid.GetNpcsInSphere(searchCoords, radius);
                NpcElement nearestNPC = null;
                float nearestSqrDistance = float.MaxValue;
                
                foreach (NpcElement nearbyNPC in nearbyNPCs) {
                    if (_visited.Contains(nearbyNPC) || nearbyNPC.AntagonismTo(owner) != shouldTarget) {
                        continue;
                    }
                    float distanceSqr = (nearbyNPC.Coords - searchCoords).sqrMagnitude;
                    if (distanceSqr < nearestSqrDistance) {
                        nearestNPC = nearbyNPC;
                        nearestSqrDistance = distanceSqr;
                    }
                }
                
                if (nearestNPC == null) {
                    return result;
                }
                
                searchCoords = nearestNPC.Coords;
                _visited.Add(nearestNPC);
                result.Add(nearestNPC);
                radius *= 1 - chainRangeDecreasePerLink;
            }

            return result;
        }

        protected override ValueOutput Payload()  => ValueOutput(typeof(NpcElement), "npcElement");
    }
}