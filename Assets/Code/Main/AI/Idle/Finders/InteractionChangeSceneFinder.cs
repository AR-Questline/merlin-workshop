using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionChangeSceneFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionChangeSceneFinder;

        ChangeSceneInteraction _changeSceneInteraction;
        ChangeSceneInteraction ChangeSceneInteraction => _changeSceneInteraction;
        public override INpcInteraction Interaction(NpcElement npc) => ChangeSceneInteraction;

        public SceneReference Scene => _changeSceneInteraction.Scene;
        
        public InteractionChangeSceneFinder(SceneReference scene) {
            _changeSceneInteraction = new ChangeSceneInteraction(scene);
        }

        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => _changeSceneInteraction.GetPortalPosition(behaviours.Npc);
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}