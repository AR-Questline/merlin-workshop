using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Profiling;

namespace Pathfinding.PID {
	using Pathfinding.Drawing;
	using Pathfinding.Util;
	using Palette = Pathfinding.Drawing.Palette.Colorbrewer.Set1;
	using Unity.Jobs;
	using Unity.Profiling;
	using UnityEngine.Assertions;
	using Unity.Burst;
	using Unity.Collections.LowLevel.Unsafe;
	using Pathfinding.RVO;

	/// <summary>Core control loop for the <see cref="FollowerEntity"/> movement script</summary>
	[System.Serializable]
	[BurstCompile]
	public struct PIDMovement {
		public struct PersistentState {
			public float maxDesiredWallDistance;
		}

		/// <summary>
		/// Desired rotation speed in degrees per second.
		///
		/// If the agent is in an open area and gets a new destination directly behind itself, it will start to rotate around with exactly this rotation speed.
		///
		/// The agent will slow down its rotation speed as it approaches its desired facing direction.
		/// So for example, when it is only 90 degrees away from its desired facing direction, it will only rotate with about half this speed.
		///
		/// See: <see cref="maxRotationSpeed"/>
		/// </summary>
		public float rotationSpeed;

		/// <summary>
		/// Desired speed of the agent in meters per second.
		///
		/// This will be multiplied by the agent's scale to get the actual speed.
		/// </summary>
		public float speed;

		/// <summary>
		/// Maximum rotation speed in degrees per second.
		///
		/// If the agent would have to rotate faster than this, it will instead slow down to get more time to rotate.
		///
		/// The agent may want to rotate faster than <see cref="rotationSpeed"/> if there's not enough space, so that it has to move in a more narrow arc.
		/// It may also want to rotate faster if it is very close to its destination and it wants to make sure it ends up on the right spot without any circling.
		///
		/// It is recommended to keep this at a value slightly larger than <see cref="rotationSpeed"/>.
		///
		/// See: <see cref="rotationSpeed"/>
		/// </summary>
		public float maxRotationSpeed;

		/// <summary>
		/// Maximum rotation speed in degrees per second while rotating on the spot.
		///
		/// Only used if <see cref="allowRotatingOnSpot"/> is enabled.
		/// </summary>
		public float maxOnSpotRotationSpeed;

		/// <summary>
		/// Time for the agent to slow down to a complete stop when it approaches the destination point, in seconds.
		///
		/// One can calculate the deceleration like: <see cref="speed"/>/<see cref="slowdownTime"/> (with units m/s^2).
		/// </summary>
		public float slowdownTime;

		/// <summary>
		/// Time for the agent to slow down to a complete stop when rotating on the spot.
		///
		/// If set to zero, the agent will instantly stop and start to turn around.
		///
		/// Only used if <see cref="allowRotatingOnSpot"/> is enabled.
		/// </summary>
		public float slowdownTimeWhenTurningOnSpot;

		/// <summary>
		/// How big of a distance to try to keep from obstacles.
		///
		/// Typically around 1 or 2 times the agent radius is a good value for this.
		///
		/// Try to avoid making it so large that there might not be enough space for the agent to keep this amount of distance from obstacles.
		/// It may start to move less optimally if it is not possible to keep this distance.
		///
		/// This works well in open spaces, but if your game consists of a lot of tight corridors, a low, or zero value may be better.
		///
		/// This will be multiplied by the agent's scale to get the actual distance.
		/// </summary>
		public float desiredWallDistance;

		/// <summary>
		/// How wide of a turn to make when approaching a destination for which a desired facing direction has been set.
		///
		/// The following video shows three agents, one with no facing direction set, and then two agents with varying values of the lead in radius.
		/// [Open online documentation to see videos]
		///
		/// Setting this to zero will make the agent move directly to the end of the path and rotate on the spot to face the desired facing direction, once it is there.
		///
		/// When approaching a destination for which no desired facing direction has been set, this field has no effect.
		///
		/// Warning: Setting this to a too small (but non-zero) value may look bad if the agent cannot rotate fast enough to stay on the arc.
		///
		/// This will be multiplied by the agent's scale to get the actual radius.
		/// </summary>
		public float leadInRadiusWhenApproachingDestination;

		/// <summary>
		/// If rotation on the spot is allowed or not.
		///
		/// When the agent wants to turn significantly, enabling this will make it turn on the spot instead of moving in an arc.
		/// This can make for more responsive and natural movement for humanoid characters.
		/// </summary>
		public bool allowRotatingOnSpot {
			get => allowRotatingOnSpotBacking != 0;
			set => allowRotatingOnSpotBacking = (byte)(value ? 1 : 0);
		}

		/// <summary>
		/// If rotation on the spot is allowed or not.
		/// 1 for allowed, 0 for not allowed.
		///
		/// That we have to use a byte instead of a boolean is due to a Burst limitation.
		/// </summary>
		[SerializeField]
		byte allowRotatingOnSpotBacking;

		public const float DESTINATION_CLEARANCE_FACTOR = 4f;

		private static readonly ProfilerMarker MarkerSidewaysAvoidance = new ProfilerMarker("SidewaysAvoidance");
		private static readonly ProfilerMarker MarkerPID = new ProfilerMarker("PID");
		private static readonly ProfilerMarker MarkerOptimizeDirection = new ProfilerMarker("OptimizeDirection");
		private static readonly ProfilerMarker MarkerSmallestDistance = new ProfilerMarker("ClosestDistance");
		private static readonly ProfilerMarker MarkerConvertObstacles = new ProfilerMarker("ConvertObstacles");

		[System.Flags]
		public enum DebugFlags {
			Nothing = 0,
			Position = 1 << 0,
			Tangent = 1 << 1,
			SidewaysClearance = 1 << 2,
			ForwardClearance = 1 << 3,
			Obstacles = 1 << 4,
			Funnel = 1 << 5,
			Path = 1 << 6,
			ApproachWithOrientation = 1 << 7,
			Rotation = 1 << 8,
		}

		public void ScaleByAgentScale (float agentScale) {
        }

        public float Speed (float remainingDistance) {
            return default;
        }

        /// <summary>
        /// Accelerates as quickly as possible.
        ///
        /// This follows the same curve as the <see cref="Speed"/> function, as a function of the remaining distance.
        ///
        /// Returns: The speed the agent should have after accelerating for dt seconds. Assuming dt is small.
        /// </summary>
        /// <param name="speed">The current speed of the agent.</param>
        /// <param name="timeToReachMaxSpeed">The time it takes for the agent to reach the maximum speed, starting from a standstill.</param>
        /// <param name="dt">The time to accelerate for. Can be negative to decelerate instead.</param>
        public float Accelerate (float speed, float timeToReachMaxSpeed, float dt) {
            return default;
        }

        public float CurveFollowingStrength (float signedDistToClearArea, float radiusToWall, float remainingDistance) {
            return default;
        }

        static bool ClipLineByHalfPlaneX(ref float2 a, ref float2 b, float x, float side)
        {
            return default;
        }

        static void ClipLineByHalfPlaneYt(float2 a, float2 b, float y, float side, ref float mnT, ref float mxT)
        {
        }

        /// <summary>
        /// Returns either the most clockwise, or most counter-clockwise direction of the three given directions.
        /// The directions are compared pairwise, not using any global reference angle.
        /// </summary>
        static float2 MaxAngle (float2 a, float2 b, float2 c, bool clockwise) {
            return default;
        }

        /// <summary>
        /// Returns either the most clockwise, or most counter-clockwise direction of the two given directions.
        /// The directions are compared pairwise, not using any global reference angle.
        /// </summary>
        static float2 MaxAngle (float2 a, float2 b, bool clockwise) {
            return default;
        }

        const float ALLOWED_OVERLAP_FACTOR = 0.1f;
		const float STEP_MULTIPLIER = 1.0f;
		const float MAX_FRACTION_OF_REMAINING_DISTANCE = 0.9f;
		const int OPTIMIZATION_ITERATIONS = 8;

		static void DrawChisel (float2 start, float2 direction, float pointiness, float length, float width, CommandBuilder draw, Color col)
        {
        }

        static void SplitSegment(float2 e1, float2 e2, float desiredRadius, float length, float pointiness, ref EdgeBuffers buffers)
        {
        }

        static void SplitSegment2(float2 e1, float2 e2, float desiredRadius, float pointiness, ref EdgeBuffers buffers)
        {
        }

        static void SplitSegment3(float2 e1, float2 e2, float desiredRadius, bool inTriangularRegion, ref EdgeBuffers buffers)
        {
        }

        static void SplitSegment4(float2 e1, float2 e2, bool inTriangularRegion, bool left, ref EdgeBuffers buffers)
        {
        }

        private struct EdgeBuffers
        {
            public FixedList512Bytes<float2> triangleRegionEdgesL;
            public FixedList512Bytes<float2> triangleRegionEdgesR;
            public FixedList512Bytes<float2> straightRegionEdgesL;
            public FixedList512Bytes<float2> straightRegionEdgesR;
		}

        /// <summary>
        /// Finds a direction to move in that is as close as possible to the desired direction while being clear of obstacles, if possible.
        /// This keeps the agent from moving too close to walls.
        /// </summary>
        /// <param name="start">Current position of the agent.</param>
        /// <param name="end">Point the agent is moving towards.</param>
        /// <param name="desiredRadius">The distance the agent should try to keep from obstacles.</param>
        /// <param name="remainingDistance">Remaining distance in the path.</param>
        /// <param name="pointiness">Essentially controls how much the agent will cut corners. A higher value will lead to a smoother path,
        ///        but it will also lead to the agent not staying as far away from corners as the desired wall distance parameter would suggest.
        ///        It is a unitless quantity.</param>
        /// <param name="edges">Edges of obstacles. Each edge is represented by two points.</param>
        /// <param name="draw">CommandBuilder to use for drawing debug information.</param>
        /// <param name="debugFlags">Flags to control what debug information to draw.</param>
        public static float2 OptimizeDirection (float2 start, float2 end, float desiredRadius, float remainingDistance, float pointiness, NativeArray<float2> edges, CommandBuilder draw, DebugFlags debugFlags)
        {
            return default;
        }

        /// <summary>
        /// Calculates the closest point on any point of an edge that is inside a wedge.
        ///
        /// Returns: The distance to the closest point on any edge that is inside the wedge.
        /// </summary>
        /// <param name="point">The origin point of the wedge (the pointy end).</param>
        /// <param name="dir1">The first direction of the wedge.</param>
        /// <param name="dir2">The second direction of the wedge.</param>
        /// <param name="shrinkAmount">The wedge is shrunk by this amount. In the same units as the input points.</param>
        /// <param name="edges">The edges to check for intersection with.</param>
        public static float SmallestDistanceWithinWedge(float2 point, float2 dir1, float2 dir2, float shrinkAmount, NativeArray<float2> edges)
        {
            return default;
        }

        public static float2 Linecast(float2 a, float2 b, NativeArray<float2> edges)
        {
            return default;
        }

        public struct ControlParams
        {
            public Vector3 p;
            public float speed;
            public float rotation;
            public float maxDesiredWallDistance;
            public float3 endOfPath;
            public float3 facingDirectionAtEndOfPath;
            public NativeArray<float2> edges;
            public float3 nextCorner;
            public float agentRadius;
            public float remainingDistance;
            public float3 closestOnNavmesh;
            public DebugFlags debugFlags;
			public NativeMovementPlane movementPlane;
        }

        /// <summary>
        /// Finds the bounding box in which this controller is interested in navmesh edges.
        ///
        /// The edges should be assigned to <see cref="ControlParams.edges"/>.
        /// The bounding box is relative to the given movement plane.
        /// </summary>
        public static Bounds InterestingEdgeBounds(ref PIDMovement settings, float3 position, float3 nextCorner, float height, NativeMovementPlane plane)
        {
            return default;
        }

        static float2 OffsetCornerForApproach(float2 position2D, float2 endOfPath2D, float2 facingDir2D, ref PIDMovement settings, float2 nextCorner2D, ref float gammaAngle, ref float gammaAngleWeight, DebugFlags debugFlags, ref CommandBuilder draw, NativeArray<float2> edges)
        {
            return default;
        }

        public static AnglePIDControlOutput2D Control(ref PIDMovement settings, float dt, ref ControlParams controlParams, ref CommandBuilder draw, out float maxDesiredWallDistance)
        {
            maxDesiredWallDistance = default(float);
            return default;
        }
    }

    /// <summary>
    /// Implements a PID controller for the angular velocity of an agent following a curve.
    ///
    /// The PID controller is formulated for small angles (see https://en.wikipedia.org/wiki/Small-angle_approximation), but extends well to large angles.
    /// For small angles, if y(t) is the curve/agent position, then y'(t) is the angle and y''(t) is the angular velocity.
    /// This controller outputs an angular velocity, meaning it controls y''(t).
    ///
    /// See https://en.wikipedia.org/wiki/PID_controller
    /// </summary>
    public static class AnglePIDController
    {
        const float DampingRatio = 1.0f;

        /// <summary>
        /// An approximate turning radius the agent will have in an open space.
        ///
        /// This is based on the PID controller in the <see cref="Control"/> method.
        /// </summary>
        public static float ApproximateTurningRadius(float followingStrength)
        {
            return default;
        }

        /// <summary>
        /// Given a speed and a rotation speed, what is the approximate corresponding following strength.
        ///
        /// This is based on the PID controller in the <see cref="Control"/> method.
        /// </summary>
        public static float RotationSpeedToFollowingStrength(float speed, float maxRotationSpeed)
        {
            return default;
        }

        public static float FollowingStrengthToRotationSpeed(float followingStrength)
        {
            return default;
        }

        /// <summary>
        /// How much to rotate and move in order to smoothly follow a given curve.
        ///
        /// If the maximum rotation speed (settings.maxRotationSpeed) would be exceeded, the agent will slow down to avoid exceeding it (up to a point).
        ///
        /// Returns: A control value that can be used to move the agent.
        /// </summary>
        /// <param name="settings">Various movement settings</param>
        /// <param name="followingStrength">The integral term of the PID controller. The higher this value is, the quicker the agent will try to align with the curve.</param>
        /// <param name="angle">The current direction of the agent, in radians.</param>
        /// <param name="curveAngle">The angle of the curve tangent at the nearest point, in radians.</param>
        /// <param name="curveCurvature">The curvature of the curve at the nearest point. Positive values means the curve is turning to the left, negative values means the curve is turning to the right.</param>
        /// <param name="curveDistanceSigned">The signed distance from the agent to the curve. Positive values means the agent is to the right of the curve, negative values means the agent is to the left of the curve.</param>
        /// <param name="speed">How quickly the agent should move. In meters/second.</param>
        /// <param name="remainingDistance">The remaining distance to where the agent should stop. In meters.</param>
        /// <param name="minRotationSpeed">The minimum rotation speed of the agent. In radians/second. Unless the agent does not desire to rotate at all, it will rotate at least this fast.</param>
        /// <param name="isStationary">Should be true if the agent is currently standing still (or close to it). This allows it to rotate in place.</param>
        /// <param name="dt">How long the current time-step is. In seconds.</param>
        public static AnglePIDControlOutput2D Control(ref PIDMovement settings, float followingStrength, float angle, float curveAngle, float curveCurvature, float curveDistanceSigned, float speed, float remainingDistance, float minRotationSpeed, bool isStationary, float dt)
        {
            return default;
        }
    }
}
