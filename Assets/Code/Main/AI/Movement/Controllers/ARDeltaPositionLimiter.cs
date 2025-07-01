using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility;
using Newtonsoft.Json;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.Controllers {
    public sealed partial class ARDeltaPositionLimiter : Element<NpcElement>, IDeltaPositionLimiter {
        public override ushort TypeForSerialization => SavedModels.ARDeltaPositionLimiter;

        float MinDistanceToTarget => DistancesToTargetHandler.MinDistanceToTarget(ParentModel);
        
        public Vector2 LimitDeltaPosition(Vector3 currentPosition, Vector2 currentDeltaPosition) {
            if (ParentModel == null || ParentModel.HasBeenDiscarded) {
                return currentDeltaPosition;
            }

            var minDistanceToTarget = MinDistanceToTarget;

            if (minDistanceToTarget <= 0) {
                return currentDeltaPosition;
            }
            
            ICharacter currentTarget = ParentModel.GetCurrentTarget();
            Vector3 direction = AIUtils.LimitDeltaPositionTowardsTarget(currentTarget, currentPosition, currentDeltaPosition.ToHorizontal3(), minDistanceToTarget);
            return direction.ToHorizontal2();
        }
    }
}