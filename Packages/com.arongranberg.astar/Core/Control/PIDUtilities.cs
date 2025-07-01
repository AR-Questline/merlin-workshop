using Unity.Mathematics;

namespace Pathfinding.PID {
	public struct AnglePIDControlOutput2D {
		/// <summary>How much to rotate in a single time-step. In radians.</summary>
		public float rotationDelta;
		public float targetRotation;
		/// <summary>How much to move in a single time-step. In world units.</summary>
		public float2 positionDelta;

		public AnglePIDControlOutput2D(float currentRotation, float targetRotation, float rotationDelta, float moveDistance) : this()
        {
        }

        public static AnglePIDControlOutput2D WithMovementAtEnd (float currentRotation, float targetRotation, float rotationDelta, float moveDistance) {
            return default;
        }
    }
}
