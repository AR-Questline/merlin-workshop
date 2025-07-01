using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Grounds;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders
{
    public interface IInteractionSource {
        public const float DefaultStandInteractionDuration = 9f;
        IInteractionFinder Finder { get; }

        INpcInteraction GetFallbackInteraction(IdleBehaviours behaviours, IIdleDataSource dataSource) {
            return new StandInteraction(IdlePosition.World(Ground.SnapNpcToGround(Finder.GetDesiredPosition(behaviours) + Vector3.up)),
                IdlePosition.Self, dataSource, DefaultStandInteractionDuration);
        }
    }
}
