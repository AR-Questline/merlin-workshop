using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public interface IBehavioursOwner : IElement<Location> {
        NpcElement NpcElement { get; }
        NpcAI NpcAI { get; }
        NpcMovement NpcMovement { get; }
        ARNpcAnimancer NpcAnimancer { get; }
        float DistanceToTarget { get; }
        bool StartBehaviour(IBehaviourBase behaviour);
        void StopCurrentBehaviour(bool selectNew);
        void TriggerAnimationEvent(ARAnimationEvent animationEvent);
    }
}