using System.Collections.Generic;
using System.Linq;
using Pathfinding;

namespace Awaken.TG.Main.AI.Movement {
    public static class MovementUtils {
        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<CharacterPlace> AsWanderPath(this Path path, float turnRadius, CharacterPlace destination) {
            foreach (var point in path.vectorPath.Skip(1)) {
                if (destination.Contains(point)) {
                    yield return destination;
                    yield break;
                } else {
                    yield return new CharacterPlace(point, turnRadius);
                }
            }
        }
    }
}