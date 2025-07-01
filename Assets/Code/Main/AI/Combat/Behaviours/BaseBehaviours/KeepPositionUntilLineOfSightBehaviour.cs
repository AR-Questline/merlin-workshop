using System;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class KeepPositionUntilLineOfSightBehaviour : KeepPositionBehaviour {
        [SerializeField] float lineOfSightDistance = 10f;
        [SerializeField] bool tryToKeepMinimumDistance = true;
        [SerializeField] float minimumDistance = 5f;

        protected override bool InCorrectPosition {
            get {
                var target = ParentModel.NpcElement.GetCurrentTarget();
                return InCorrectRange(target) && IsInLineOfSight(target);
            }
        }

        public override bool UseConditionsEnsured() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            return !IsInLineOfSight(target) || !InCorrectRange(target);
        }

        bool IsInLineOfSight(ICharacter target) {
            return ParentModel.NpcElement.NpcAI.CanSee(target.AIEntity) != VisibleState.Covered;
        }
        bool InCorrectRange(ICharacter target) {
            var sqrDistance = (target.Coords - ParentModel.Coords).sqrMagnitude;
            return (sqrDistance < lineOfSightDistance * lineOfSightDistance) &&
                   (!tryToKeepMinimumDistance || sqrDistance > minimumDistance * minimumDistance);
        }
        
        protected override CharacterPlace GetTargetPosition() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            var targetCoords = target.Coords;
            var position = targetCoords + (ParentModel.Coords - targetCoords).normalized * minimumDistance;
            return new CharacterPlace(position, TargetPositionAcceptRange);
        }
    }
}
