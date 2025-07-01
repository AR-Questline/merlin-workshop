using Awaken.TG.Main.AI.Movement.Controllers;

namespace Awaken.TG.Main.AI.Movement.States {
    public class ShieldManKeepPosition : KeepPosition {
        public ShieldManKeepPosition(CharacterPlace place, VelocityScheme closeVelocity, float maxStrafeDistance,
            float distanceToChase, VelocityScheme chaseVelocity, bool invertRotation = false) : base(place,
            closeVelocity, maxStrafeDistance, distanceToChase, chaseVelocity, invertRotation) { }

        public ShieldManKeepPosition(CharacterPlace place, VelocityScheme velocity,
            float maxStrafeDistance = KeepPosition.DefaultMaxStrafeDistance) :
            base(place, velocity, maxStrafeDistance) { }

        protected override VelocityScheme DetermineVelocityScheme(bool inDistanceToChase) {
            return inDistanceToChase ? chaseVelocity : closeVelocity;
        }
    }
}