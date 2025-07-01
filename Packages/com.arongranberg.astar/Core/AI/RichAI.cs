using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.RVO;
	using Pathfinding.Util;
	using Pathfinding.Drawing;

	[AddComponentMenu("Pathfinding/AI/RichAI (3D, for navmesh)")]
	[UniqueComponent(tag = "ai")]
	[DisallowMultipleComponent]
	/// <summary>
	/// Advanced AI for navmesh based graphs.
	///
	/// See: movementscripts (view in online documentation for working links)
	/// </summary>
	public partial class RichAI : AIBase, IAstarAI {
		// === AR Code
		ITimeProvider _customTimeProvider;
		IDeltaPositionLimiter _deltaPositionLimiter = new DefaultDeltaPositionLimiter();

		protected override ITimeProvider CustomTimeProvider => _customTimeProvider;
		public void SetCustomTimeProvider(ITimeProvider timeProvider) {
        }

        public void SetCustomVelocitClamper(IDeltaPositionLimiter deltaPositionLimiter) {
        }

        /// <summary>
        /// Max acceleration of the agent.
        /// In world units per second per second.
        /// </summary>
        public float acceleration = 5;

		/// <summary>
		/// Max rotation speed of the agent.
		/// In degrees per second.
		/// </summary>
		public float rotationSpeed = 360;

		/// <summary>
		/// How long before reaching the end of the path to start to slow down.
		/// A lower value will make the agent stop more abruptly.
		///
		/// Note: The agent may require more time to slow down if
		/// its maximum <see cref="acceleration"/> is not high enough.
		///
		/// If set to zero the agent will not even attempt to slow down.
		/// This can be useful if the target point is not a point you want the agent to stop at
		/// but it might for example be the player and you want the AI to slam into the player.
		///
		/// Note: A value of zero will behave differently from a small but non-zero value (such as 0.0001).
		/// When it is non-zero the agent will still respect its <see cref="acceleration"/> when determining if it needs
		/// to slow down, but if it is zero it will disable that check.
		/// This is useful if the <see cref="destination"/> is not a point where you want the agent to stop.
		///
		/// \htmlonly <video class="tinyshadow" controls="true" loop="true"><source src="images/richai_slowdown_time.mp4" type="video/mp4" /></video> \endhtmlonly
		/// </summary>
		public float slowdownTime = 0.5f;

		/// <summary>
		/// Force to avoid walls with.
		/// The agent will try to steer away from walls slightly.
		///
		/// See: <see cref="wallDist"/>
		/// </summary>
		public float wallForce = 3;

		/// <summary>
		/// Walls within this range will be used for avoidance.
		/// Setting this to zero disables wall avoidance and may improve performance slightly
		///
		/// See: <see cref="wallForce"/>
		/// </summary>
		public float wallDist = 1;

		/// <summary>
		/// Use funnel simplification.
		/// On tiled navmesh maps, but sometimes on normal ones as well, it can be good to simplify
		/// the funnel as a post-processing step to make the paths straighter.
		///
		/// This has a moderate performance impact during frames when a path calculation is completed.
		///
		/// The RichAI script uses its own internal funnel algorithm, so you never
		/// need to attach the FunnelModifier component.
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="Pathfinding.FunnelModifier"/>
		/// </summary>
		public bool funnelSimplification = false;

		/// <summary>
		/// Slow down when not facing the target direction.
		/// Incurs at a small performance overhead.
		///
		/// This setting only has an effect if <see cref="enableRotation"/> is enabled.
		/// </summary>
		public bool slowWhenNotFacingTarget = true;

		/// <summary>
		/// Prevent the velocity from being too far away from the forward direction of the character.
		/// If the character is ordered to move in the opposite direction from where it is facing
		/// then enabling this will cause it to make a small loop instead of turning on the spot.
		///
		/// This setting only has an effect if <see cref="slowWhenNotFacingTarget"/> is enabled.
		/// </summary>
		public bool preventMovingBackwards = false;

		/// <summary>
		/// Called when the agent starts to traverse an off-mesh link.
		/// Register to this callback to handle off-mesh links in a custom way.
		///
		/// If this event is set to null then the agent will fall back to traversing
		/// off-mesh links using a very simple linear interpolation.
		///
		/// <code>
		/// void OnEnable () {
		///     ai = GetComponent<RichAI>();
		///     if (ai != null) ai.onTraverseOffMeshLink += TraverseOffMeshLink;
		/// }
		///
		/// void OnDisable () {
		///     if (ai != null) ai.onTraverseOffMeshLink -= TraverseOffMeshLink;
		/// }
		///
		/// IEnumerator TraverseOffMeshLink (RichSpecial link) {
		///     // Traverse the link over 1 second
		///     float startTime = Time.time;
		///
		///     while (Time.time < startTime + 1) {
		///         transform.position = Vector3.Lerp(link.first.position, link.second.position, Time.time - startTime);
		///         yield return null;
		///     }
		///     transform.position = link.second.position;
		/// }
		/// </code>
		/// </summary>
		public System.Func<RichSpecial, IEnumerator> onTraverseOffMeshLink;

		/// <summary>Holds the current path that this agent is following</summary>
		protected readonly RichPath richPath = new RichPath();

		protected bool delayUpdatePath;
		protected bool lastCorner;

		/// <summary>Internal state used for filtering out noise in the agent's rotation</summary>
		Vector2 rotationFilterState;
		Vector2 rotationFilterState2;

		/// <summary>Distance to <see cref="steeringTarget"/> in the movement plane</summary>
		protected float distanceToSteeringTarget = float.PositiveInfinity;

		protected readonly List<Vector3> nextCorners = new List<Vector3>();
		protected readonly List<Vector3> wallBuffer = new List<Vector3>();

		public bool traversingOffMeshLink { get; protected set; }

		/// <summary>\copydoc Pathfinding::IAstarAI::remainingDistance</summary>
		public float remainingDistance {
			get {
				return distanceToSteeringTarget + Vector3.Distance(steeringTarget, richPath.Endpoint);
			}
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::reachedEndOfPath</summary>
		public bool reachedEndOfPath { get { return approachingPathEndpoint && distanceToSteeringTarget < endReachedDistance; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::reachedDestination</summary>
		public override bool reachedDestination {
			get {
				if (!reachedEndOfPath) return false;
				// Distance from our position to the current steering target +
				// Distance from the steering target to the end of the path +
				// distance from the end of the path to the destination.
				// Note that most distance checks are done only in the movement plane (which means in most cases that the y coordinate differences are discarded).
				// This is because those coordinates are often not very accurate.
				// A separate check is done below to make sure that the destination y coordinate is correct
				if (distanceToSteeringTarget + movementPlane.ToPlane(steeringTarget - richPath.Endpoint).magnitude + movementPlane.ToPlane(destination - richPath.Endpoint).magnitude > endReachedDistance) return false;

				// Don't do height checks in 2D mode
				if (orientation != OrientationMode.YAxisForward) {
					// Check if the destination is above the head of the character or far below the feet of it
					float yDifference;
					movementPlane.ToPlane(destination - position, out yDifference);
					var h = tr.localScale.y * height;
					if (yDifference > h || yDifference < -h*0.5) return false;
				}

				return true;
			}
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::hasPath</summary>
		public bool hasPath { get { return richPath.GetCurrentPart() != null; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::pathPending</summary>
		public bool pathPending { get { return waitingForPathCalculation || delayUpdatePath; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::steeringTarget</summary>
		public Vector3 steeringTarget { get; protected set; }

		/// <summary>\copydoc Pathfinding::IAstarAI::radius</summary>
		float IAstarAI.radius { get { return radius; } set { radius = value; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::height</summary>
		float IAstarAI.height { get { return height; } set { height = value; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::maxSpeed</summary>
		float IAstarAI.maxSpeed { get { return maxSpeed; } set { maxSpeed = value; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::canSearch</summary>
		bool IAstarAI.canSearch { get { return canSearch; } set { canSearch = value; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::canMove</summary>
		bool IAstarAI.canMove { get { return canMove; } set { canMove = value; } }

		/// <summary>\copydoc Pathfinding::IAstarAI::movementPlane</summary>
		NativeMovementPlane IAstarAI.movementPlane => new NativeMovementPlane(movementPlane);

		/// <summary>
		/// True if approaching the last waypoint in the current part of the path.
		/// Path parts are separated by off-mesh links.
		///
		/// See: <see cref="approachingPathEndpoint"/>
		/// </summary>
		public bool approachingPartEndpoint {
			get {
				return lastCorner && nextCorners.Count == 1;
			}
		}

		/// <summary>
		/// True if approaching the last waypoint of all parts in the current path.
		/// Path parts are separated by off-mesh links.
		///
		/// See: <see cref="approachingPartEndpoint"/>
		/// </summary>
		public bool approachingPathEndpoint {
			get {
				return approachingPartEndpoint && richPath.IsLastPart;
			}
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::endOfPath</summary>
		public override Vector3 endOfPath {
			get {
				if (hasPath) return richPath.Endpoint;
				if (float.IsFinite(destination.x)) return destination;
				return position;
			}
		}

		public override Quaternion rotation {
			get {
				return base.rotation;
			}
			set {
				base.rotation = value;
				// Make the agent keep this rotation instead of just rotating back to whatever it used before
				rotationFilterState = Vector2.zero;
				rotationFilterState2 = Vector2.zero;
			}
		}

		/// <summary>
		/// \copydoc Pathfinding::IAstarAI::Teleport
		///
		/// When setting transform.position directly the agent
		/// will be clamped to the part of the navmesh it can
		/// reach, so it may not end up where you wanted it to.
		/// This ensures that the agent can move to any part of the navmesh.
		/// </summary>
		public override void Teleport (Vector3 newPosition, bool clearPath = true) {
        }

        protected virtual Vector3 ClampPositionToGraph (Vector3 newPosition) {
            return default;
        }

        public void ForceTeleport(Vector3 newPosition) {
        }

        /// <summary>Called when the component is disabled</summary>
        protected override void OnDisable () {
        }

        protected override bool shouldRecalculatePath {
			get {
				// Don't automatically recalculate the path in the middle of an off-mesh link
				return base.shouldRecalculatePath && !traversingOffMeshLink;
			}
		}

		public override void SearchPath () {
        }

        protected override void OnPathComplete (Path p) {
        }

        protected override void ClearPath () {
        }

        /// <summary>
        /// Declare that the AI has completely traversed the current part.
        /// This will skip to the next part, or call OnTargetReached if this was the last part
        /// </summary>
        protected void NextPart () {
        }

        /// <summary>\copydocref{IAstarAI.GetRemainingPath(List<Vector3>,bool)}</summary>
        public void GetRemainingPath (List<Vector3> buffer, out bool stale) {
            stale = default(bool);
        }

        /// <summary>\copydocref{IAstarAI.GetRemainingPath(List<Vector3>,List<PathPartWithLinkInfo>,bool)}</summary>
        public void GetRemainingPath (List<Vector3> buffer, List<PathPartWithLinkInfo> partsBuffer, out bool stale) {
            stale = default(bool);
        }

        /// <summary>
        /// Called when the end of the path is reached.
        ///
        /// Deprecated: Avoid overriding this method. Instead poll the <see cref="reachedDestination"/> or <see cref="reachedEndOfPath"/> properties.
        /// </summary>
        protected virtual void OnTargetReached () {
        }

        protected virtual Vector3 UpdateTarget (RichFunnel fn) {
            return default;
        }

        /// <summary>Called during either Update or FixedUpdate depending on if rigidbodies are used for movement or not</summary>
        protected override void MovementUpdateInternal(float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation)
        {
            nextPosition = default(Vector3);
            nextRotation = default(Quaternion);
        }

        void TraverseFunnel(RichFunnel fn, float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation)
        {
            nextPosition = default(Vector3);
            nextRotation = default(Quaternion);
        }

        void FinalMovement(Vector3 position3D, float deltaTime, float distanceToEndOfPath, float speedLimitFactor, out Vector3 nextPosition, out Quaternion nextRotation)
        {
            nextPosition = default(Vector3);
            nextRotation = default(Quaternion);
        }

        protected override Vector3 ClampToNavmesh (Vector3 position, out bool positionChanged) {
            positionChanged = default(bool);
            return default;
        }

        Vector2 CalculateWallForce(Vector2 position, float elevation, Vector2 directionToTarget)
        {
            return default;
        }

        /// <summary>Traverses an off-mesh link</summary>
        protected virtual IEnumerator TraverseSpecial(RichSpecial link)
        {
            return default;
        }

        /// <summary>
        /// Fallback for traversing off-mesh links in case <see cref="onTraverseOffMeshLink"/> is not set.
        /// This will do a simple linear interpolation along the link.
        /// </summary>
        protected IEnumerator TraverseOffMeshLinkFallback(RichSpecial link)
        {
            return default;
        }

        protected static readonly Color GizmoColorPath = new Color(8.0f/255, 78.0f/255, 194.0f/255);

		public override void DrawGizmos () {
        }
    }
}
