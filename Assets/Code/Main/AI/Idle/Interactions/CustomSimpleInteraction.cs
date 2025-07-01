using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    /// <summary>
    /// Custom Simple Interaction is used to dynamically create interactions from code.
    /// </summary>
    public class CustomSimpleInteraction : SimpleInteractionBase {
        Vector3 _position;
        Vector3 _forward;
        
        protected override Vector3 SnapToForward => _forward;
        protected override Vector3 SnapToPosition => _position;

        public override Vector3? GetInteractionPosition(NpcElement npc) => _position;
        public override Vector3 GetInteractionForward(NpcElement npc) => _forward;
        
        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return false;
        } 
        
        public void Setup(Vector3 position, Vector3 forward, ShareableARAssetReference animationOverrides, float duration) {
            _shareableOverrides = animationOverrides;
            _position = position;
            _forward = forward;
            hasDuration = true;
            this.duration = duration;
        }
    }
}