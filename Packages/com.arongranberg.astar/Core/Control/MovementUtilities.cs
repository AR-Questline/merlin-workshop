using UnityEngine;

namespace Pathfinding.Util {
	public static class MovementUtilities {
		public static float FilterRotationDirection (ref Vector2 state, ref Vector2 state2, Vector2 deltaPosition, float threshold, float deltaTime, bool avoidingOtherAgents) {
            return default;
        }

        /// <summary>
        /// Clamps the velocity to the max speed and optionally the forwards direction.
        ///
        /// Note that all vectors are 2D vectors, not 3D vectors.
        ///
        /// Returns: The clamped velocity in world units per second.
        /// </summary>
        /// <param name="velocity">Desired velocity of the character. In world units per second.</param>
        /// <param name="maxSpeed">Max speed of the character. In world units per second.</param>
        /// <param name="speedLimitFactor">Value between 0 and 1 which determines how much slower the character should move than normal.
        ///      Normally 1 but should go to 0 when the character approaches the end of the path.</param>
        /// <param name="slowWhenNotFacingTarget">Slow the character down if the desired velocity is not in the same direction as the forward vector.</param>
        /// <param name="preventMovingBackwards">Prevent the velocity from being too far away from the forward direction of the character.</param>
        /// <param name="forward">Forward direction of the character. Used together with the slowWhenNotFacingTarget parameter.</param>
        public static Vector2 ClampVelocity(Vector2 velocity, float maxSpeed, float speedLimitFactor, bool slowWhenNotFacingTarget, bool preventMovingBackwards, Vector2 forward)
        {
            return default;
        }

        /// <summary>Calculate an acceleration to move deltaPosition units and get there with approximately a velocity of targetVelocity</summary>
        public static Vector2 CalculateAccelerationToReachPoint(Vector2 deltaPosition, Vector2 targetVelocity, Vector2 currentVelocity, float forwardsAcceleration, float rotationSpeed, float maxSpeed, Vector2 forwardsVector)
        {
            return default;
        }
    }
}
