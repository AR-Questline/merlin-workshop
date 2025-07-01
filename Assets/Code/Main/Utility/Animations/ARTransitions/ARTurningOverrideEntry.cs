using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    [Serializable]
    public class ARTurningAnimationOverrideEntry {
        public ARClipTransition clip;
        public FloatRange currentSpeedRange;
        public FloatRange turningAngleRange;

        public bool IsInRange(NpcElement npc) {
            float currentSpeed = npc.Controller.CurrentVelocity.magnitude;
            float turningAngle = Vector2.SignedAngle(npc.Controller.LogicalForward, npc.Controller.SteeringDirection) * -1.0f;
            
            return currentSpeedRange.Contains(currentSpeed) && turningAngleRange.Contains(turningAngle);
        }
    }
}