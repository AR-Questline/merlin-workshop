using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace Pathfinding {
	using Pathfinding.RVO;
	using Pathfinding.Util;
	using Pathfinding.Jobs;
	using Pathfinding.Drawing;
	using UnityEngine.Jobs;

	/// <summary>
	/// Base class for AIPath and RichAI.
	/// This class holds various methods and fields that are common to both AIPath and RichAI.
	///
	/// See: <see cref="Pathfinding.AIPath"/>
	/// See: <see cref="Pathfinding.RichAI"/>
	/// See: <see cref="Pathfinding.IAstarAI"/> (all movement scripts implement this interface)
	/// </summary>
	[RequireComponent(typeof(Seeker))]
	public abstract class AIBase : VersionedMonoBehaviour {
		// === AR Code
		const float GroundedCheckRadius = 0.2f;
		readonly ITimeProvider _unityTimeProvider = new UnityTimeProvider();
		protected virtual ITimeProvider CustomTimeProvider { get; }
		ITimeProvider TimeProvider => CustomTimeProvider is { IsValid: true } ? CustomTimeProvider : _unityTimeProvider;
		public bool canUseGravity;

		/// <summary>\copydoc Pathfinding::IAstarAI::radius</summary>
		public float radius = 0.5f;

		/// <summary>\copydocref{IAstarAI.height}</summary>
		public float height = 2;

		/// <summary>
		/// Determines how often the agent will search for new paths (in seconds).
		/// The agent will plan a new path to the target every N seconds.
		///
		/// If you have fast moving targets or AIs, you might want to set it to a lower value.
		///
		/// See: <see cref="shouldRecalculatePath"/>
		/// See: <see cref="SearchPath"/>
		///
		/// Deprecated: This has been renamed to <see cref="autoRepath.period"/>.
		/// See: <see cref="AutoRepathPolicy"/>
		/// </summary>
		public float repathRate {
			get {
				return this.autoRepath.period;
			}
			set {
				this.autoRepath.period = value;
			}
		}

		/// <summary>
		/// \copydocref{IAstarAI::canSearch}
		/// Deprecated: This has been superseded by <see cref="autoRepath.mode"/>.
		/// </summary>
		public bool canSearch {
			get {
				return this.autoRepath.mode != AutoRepathPolicy.Mode.Never;
			}
			set {
				if (value) {
					if (this.autoRepath.mode == AutoRepathPolicy.Mode.Never) {
						this.autoRepath.mode = AutoRepathPolicy.Mode.EveryNSeconds;
					}
				} else {
					this.autoRepath.mode = AutoRepathPolicy.Mode.Never;
				}
			}
		}

		/// <summary>\copydocref{IAstarAI.canMove}</summary>
		public bool canMove = true;

		/// <summary>Max speed in world units per second</summary>
		[UnityEngine.Serialization.FormerlySerializedAs("speed")]
		public float maxSpeed = 1;

		/// <summary>
		/// Gravity to use.
		/// If set to (NaN,NaN,NaN) then Physics.Gravity (configured in the Unity project settings) will be used.
		/// If set to (0,0,0) then no gravity will be used and no raycast to check for ground penetration will be performed.
		/// </summary>
		public Vector3 gravity = new Vector3(float.NaN, float.NaN, float.NaN);

		/// <summary>
		/// Layer mask to use for ground placement.
		/// Make sure this does not include the layer of any colliders attached to this gameobject.
		///
		/// See: <see cref="gravity"/>
		/// See: https://docs.unity3d.com/Manual/Layers.html
		/// </summary>
		public LayerMask groundMask = -1;

		/// <summary>
		/// Distance to the end point to consider the end of path to be reached.
		///
		/// When the end of the path is within this distance then <see cref="IAstarAI.reachedEndOfPath"/> will return true.
		/// When the <see cref="destination"/> is within this distance then <see cref="IAstarAI.reachedDestination"/> will return true.
		///
		/// Note that the <see cref="destination"/> may not be reached just because the end of the path was reached. The <see cref="destination"/> may not be reachable at all.
		///
		/// See: <see cref="IAstarAI.reachedEndOfPath"/>
		/// See: <see cref="IAstarAI.reachedDestination"/>
		/// </summary>
		public float endReachedDistance = 0.2f;

		/// <summary>
		/// What to do when within <see cref="endReachedDistance"/> units from the destination.
		/// The character can either stop immediately when it comes within that distance, which is useful for e.g archers
		/// or other ranged units that want to fire on a target. Or the character can continue to try to reach the exact
		/// destination point and come to a full stop there. This is useful if you want the character to reach the exact
		/// point that you specified.
		///
		/// Note: <see cref="IAstarAI.reachedEndOfPath"/> will become true when the character is within <see cref="endReachedDistance"/> units from the destination
		/// regardless of what this field is set to.
		/// </summary>
		public CloseToDestinationMode whenCloseToDestination = CloseToDestinationMode.Stop;

		/// <summary>
		/// Controls if the agent slows down to a stop if the area around the destination is crowded.
		///
		/// Using this module requires that local avoidance is used: i.e. that an RVOController is attached to the GameObject.
		///
		/// See: <see cref="Pathfinding.RVO.RVODestinationCrowdedBehavior"/>
		/// See: local-avoidance (view in online documentation for working links)
		/// </summary>
		public RVODestinationCrowdedBehavior rvoDensityBehavior = new RVODestinationCrowdedBehavior(true, 0.5f, false);

		[SerializeField]
		[HideInInspector]
		[UnityEngine.Serialization.FormerlySerializedAs("repathRate")]
		float repathRateCompatibility = float.NaN;

		[SerializeField]
		[HideInInspector]
		[UnityEngine.Serialization.FormerlySerializedAs("canSearch")]
		[UnityEngine.Serialization.FormerlySerializedAs("repeatedlySearchPaths")]
		bool canSearchCompability = false;

		/// <summary>
		/// Determines which direction the agent moves in.
		/// For 3D games you most likely want the ZAxisIsForward option as that is the convention for 3D games.
		/// For 2D games you most likely want the YAxisIsForward option as that is the convention for 2D games.
		///
		/// Using the YAxisForward option will also allow the agent to assume that the movement will happen in the 2D (XY) plane instead of the XZ plane
		/// if it does not know. This is important only for the point graph which does not have a well defined up direction. The other built-in graphs (e.g the grid graph)
		/// will all tell the agent which movement plane it is supposed to use.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[UnityEngine.Serialization.FormerlySerializedAs("rotationIn2D")]
		public OrientationMode orientation = OrientationMode.ZAxisForward;

		/// <summary>
		/// If true, the AI will rotate to face the movement direction.
		/// See: <see cref="orientation"/>
		/// </summary>
		public bool enableRotation = true;

		/// <summary>
		/// Position of the agent.
		/// If <see cref="updatePosition"/> is true then this value will be synchronized every frame with Transform.position.
		/// </summary>
		protected Vector3 simulatedPosition;

		/// <summary>
		/// Rotation of the agent.
		/// If <see cref="updateRotation"/> is true then this value will be synchronized every frame with Transform.rotation.
		/// </summary>
		protected Quaternion simulatedRotation;

		/// <summary>
		/// Position of the agent.
		/// In world space.
		/// If <see cref="updatePosition"/> is true then this value is idential to transform.position.
		/// See: <see cref="Teleport"/>
		/// See: <see cref="Move"/>
		/// </summary>
		public Vector3 position { get { return updatePosition ? tr.position : simulatedPosition; } }

		/// <summary>
		/// Rotation of the agent.
		/// If <see cref="updateRotation"/> is true then this value is identical to transform.rotation.
		/// </summary>
		public virtual Quaternion rotation {
			get { return updateRotation ? tr.rotation : simulatedRotation; }
			set {
				if (updateRotation) {
					tr.rotation = value;
				} else {
					simulatedRotation = value;
				}
			}
		}

		/// <summary>Accumulated movement deltas from the <see cref="Move"/> method</summary>
		protected Vector3 accumulatedMovementDelta = Vector3.zero;

		/// <summary>
		/// Current desired velocity of the agent (does not include local avoidance and physics).
		/// Lies in the movement plane.
		/// </summary>
		protected Vector2 velocity2D;

		/// <summary>
		/// Velocity due to gravity.
		/// Perpendicular to the movement plane.
		///
		/// When the agent is grounded this may not accurately reflect the velocity of the agent.
		/// It may be non-zero even though the agent is not moving.
		/// </summary>
		protected float verticalVelocity;

		/// <summary>Cached Seeker component</summary>
		protected Seeker seeker;

		/// <summary>Cached Transform component</summary>
		protected Transform tr;

		/// <summary>Cached Rigidbody component</summary>
		protected Rigidbody rigid;

		/// <summary>Cached CharacterController component</summary>
		protected CharacterController controller;

		/// <summary>Cached RVOController component</summary>
		protected RVOController rvoController;
		
		public bool hasEnabledRVOController => rvoController != null && rvoController.enabled && rvoController.gameObject.activeInHierarchy;
		/// <summary>
		/// Plane which this agent is moving in.
		/// This is used to convert between world space and a movement plane to make it possible to use this script in
		/// both 2D games and 3D games.
		/// </summary>
		public SimpleMovementPlane movementPlane = new SimpleMovementPlane(Quaternion.identity);

		/// <summary>
		/// Determines if the character's position should be coupled to the Transform's position.
		/// If false then all movement calculations will happen as usual, but the object that this component is attached to will not move
		/// instead only the <see cref="position"/> property will change.
		///
		/// This is useful if you want to control the movement of the character using some other means such
		/// as for example root motion but still want the AI to move freely.
		/// See: Combined with calling <see cref="MovementUpdate"/> from a separate script instead of it being called automatically one can take a similar approach to what is documented here: https://docs.unity3d.com/Manual/nav-CouplingAnimationAndNavigation.html
		///
		/// See: <see cref="canMove"/> which in contrast to this field will disable all movement calculations.
		/// See: <see cref="updateRotation"/>
		/// </summary>
		public bool updatePosition { get; set; } = true;

		/// <summary>
		/// Determines if the character's rotation should be coupled to the Transform's rotation.
		/// If false then all movement calculations will happen as usual, but the object that this component is attached to will not rotate
		/// instead only the <see cref="rotation"/> property will change.
		///
		/// See: <see cref="updatePosition"/>
		/// </summary>
		public bool updateRotation { get; set; } = true;

		/// <summary>
		/// Determines how the agent recalculates its path automatically.
		/// This corresponds to the settings under the "Recalculate Paths Automatically" field in the inspector.
		/// </summary>
		public AutoRepathPolicy autoRepath = new AutoRepathPolicy();

		/// <summary>Indicates if gravity is used during this frame</summary>
		protected bool usingGravity { get; set; }

		/// <summary>Delta time used for movement during the last frame</summary>
		protected float lastDeltaTime;

		/// <summary>Position of the character at the end of the last frame</summary>
		protected Vector3 prevPosition1;

		/// <summary>Position of the character at the end of the frame before the last frame</summary>
		protected Vector3 prevPosition2;

		/// <summary>Amount which the character wants or tried to move with during the last frame</summary>
		protected Vector2 lastDeltaPosition;

		/// <summary>Only when the previous path has been calculated should the script consider searching for a new path</summary>
		protected bool waitingForPathCalculation = false;

		/// <summary>Time when the last path request was started</summary>
		protected float lastRepath = float.NegativeInfinity;

		/// <summary>
		/// True if the Start method has been executed.
		/// Used to test if coroutines should be started in OnEnable to prevent calculating paths
		/// in the awake stage (or rather before start on frame 0).
		/// </summary>
		protected bool startHasRun = false;

		public bool isPaused;
		public bool canBePaused = true;
		
		/// <summary>Backing field for <see cref="destination"/></summary>
		Vector3 destinationBackingField = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

		/// <summary>\copydocref{IAstarAI.destination}</summary>
		public Vector3 destination {
			get { return destinationBackingField; }
			set {
				// Note: vector3 equality operator will return false if both are (inf,inf,inf). So do the extra check to see if both are infinity.
				if (rvoDensityBehavior.enabled && !(value == destinationBackingField || (float.IsPositiveInfinity(value.x) && float.IsPositiveInfinity(destinationBackingField.x)))) {
					destinationBackingField = value;
					rvoDensityBehavior.OnDestinationChanged(value, reachedDestination);
				} else {
					destinationBackingField = value;
				}
			}
		}

		/// <summary>\copydocref{IAstarAI.velocity}</summary>
		public Vector3 velocity {
			get {
				return lastDeltaTime > 0.000001f ? (prevPosition1 - prevPosition2) / lastDeltaTime : Vector3.zero;
			}
		}

		/// <summary>\copydocref{IAstarAI.desiredVelocity}</summary>
		public Vector3 desiredVelocity {
			get { return lastDeltaTime > 0.00001f ? movementPlane.ToWorld(lastDeltaPosition / lastDeltaTime, verticalVelocity) : Vector3.zero; }
		}

		/// <summary>\copydocref{IAstarAI.desiredVelocityWithoutLocalAvoidance}</summary>
		public Vector3 desiredVelocityWithoutLocalAvoidance {
			get { return movementPlane.ToWorld(velocity2D, verticalVelocity); }
			set { velocity2D = movementPlane.ToPlane(value, out verticalVelocity); }
		}

		/// <summary>\copydocref{IAstarAI.endOfPath}</summary>
		public abstract Vector3 endOfPath { get; }

		/// <summary>\copydocref{IAstarAI.reachedDestination}</summary>
		public abstract bool reachedDestination { get; }

		/// <summary>\copydocref{IAstarAI.isStopped}</summary>
		public bool isStopped { get; set; }

		/// <summary>\copydocref{IAstarAI.onSearchPath}</summary>
		public System.Action onSearchPath { get; set; }

		/// <summary>
		/// Cached delegate for the <see cref="OnPathComplete"/> method.
		///
		/// Caching this avoids allocating a new one every time a path is calculated, which reduces GC pressure.
		/// </summary>
		protected OnPathDelegate onPathComplete;

		/// <summary>True if the path should be automatically recalculated as soon as possible</summary>
		protected virtual bool shouldRecalculatePath {
			get {
				return !waitingForPathCalculation && autoRepath.ShouldRecalculatePath(position, radius, destination, Time.time);
			}
		}
		
		/// <summary>
		/// Looks for any attached components like RVOController and CharacterController etc.
		///
		/// This is done during <see cref="OnEnable"/>. If you are adding/removing components during runtime you may want to call this function
		/// to make sure that this script finds them. It is unfortunately prohibitive from a performance standpoint to look for components every frame.
		/// </summary>
		public virtual void FindComponents () {
        }

        /// <summary>Called when the component is enabled</summary>
        protected virtual void OnEnable () {
        }

        /// <summary>
        /// Called every frame.
        /// This may be called during FixedUpdate or Update depending on if a rigidbody is attached to the GameObject.
        /// </summary>
        static void OnUpdate (AIBase[] components, int count, TransformAccessArray transforms, BatchedEvents.Event ev) {
        }

        /// <summary>Called every frame</summary>
        protected virtual void OnUpdate (float dt) {
        }

        /// <summary>
        /// Starts searching for paths.
        /// If you override this method you should in most cases call base.Start () at the start of it.
        /// See: <see cref="Init"/>
        /// </summary>
        protected virtual void Start () {
        }

        void Init () {
        }

        public void InitPosition(Vector3 pos) {
        }

        /// <summary>\copydoc Pathfinding::IAstarAI::Teleport</summary>
        public virtual void Teleport (Vector3 newPosition, bool clearPath = true) {
        }

        protected void CancelCurrentPathRequest () {
        }

        protected virtual void OnDisable () {
        }

        /// <summary>\copydocref{IAstarAI.MovementUpdate}</summary>
        public void MovementUpdate (float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation) {
            nextPosition = default(Vector3);
            nextRotation = default(Quaternion);
        }

        /// <summary>Called during either Update or FixedUpdate depending on if rigidbodies are used for movement or not</summary>
        protected abstract void MovementUpdateInternal(float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation);

		/// <summary>
		/// Outputs the start point and end point of the next automatic path request.
		/// This is a separate method to make it easy for subclasses to swap out the endpoints
		/// of path requests. For example the <see cref="LocalSpaceRichAI"/> script which requires the endpoints
		/// to be transformed to graph space first.
		/// </summary>
		protected virtual void CalculatePathRequestEndpoints (out Vector3 start, out Vector3 end) {
            start = default(Vector3);
            end = default(Vector3);
        }

        /// <summary>\copydocref{IAstarAI.SearchPath}</summary>
        public virtual void SearchPath()
        {
        }

        /// <summary>
        /// Position of the base of the character.
        /// This is used for pathfinding as the character's pivot point is sometimes placed
        /// at the center of the character instead of near the feet. In a building with multiple floors
        /// the center of a character may in some scenarios be closer to the navmesh on the floor above
        /// than to the floor below which could cause an incorrect path to be calculated.
        /// To solve this the start point of the requested paths is always at the base of the character.
        /// </summary>
        public virtual Vector3 GetFeetPosition()
        {
            return default;
        }

        /// <summary>Called when a requested path has been calculated</summary>
        protected abstract void OnPathComplete(Path newPath);

		/// <summary>
		/// Clears the current path of the agent.
		///
		/// Usually invoked using SetPath(null).
		///
		/// See: <see cref="SetPath"/>
		/// See: <see cref="isStopped"/>
		/// </summary>
		protected abstract void ClearPath();

		/// <summary>\copydocref{IAstarAI.SetPath}</summary>
		public void SetPath (Path path, bool updateDestinationFromPath = true) {
        }

        /// <summary>
        /// Accelerates the agent downwards.
        /// See: <see cref="verticalVelocity"/>
        /// See: <see cref="gravity"/>
        /// </summary>
        protected virtual void ApplyGravity (float deltaTime) {
        }

        /// <summary>Calculates how far to move during a single frame</summary>
        protected Vector2 CalculateDeltaToMoveThisFrame (Vector3 position, float distanceToEndOfPath, float deltaTime) {
            return default;
        }

        /// <summary>
        /// Simulates rotating the agent towards the specified direction and returns the new rotation.
        ///
        /// Note that this only calculates a new rotation, it does not change the actual rotation of the agent.
        /// Useful when you are handling movement externally using <see cref="FinalizeMovement"/> but you want to use the built-in rotation code.
        ///
        /// See: <see cref="orientation"/>
        /// </summary>
        /// <param name="direction">Direction in world space to rotate towards.</param>
        /// <param name="maxDegrees">Maximum number of degrees to rotate this frame.</param>
        public Quaternion SimulateRotationTowards (Vector3 direction, float maxDegrees) {
            return default;
        }

        /// <summary>
        /// Simulates rotating the agent towards the specified direction and returns the new rotation.
        ///
        /// Note that this only calculates a new rotation, it does not change the actual rotation of the agent.
        ///
        /// See: <see cref="orientation"/>
        /// See: <see cref="movementPlane"/>
        /// </summary>
        /// <param name="direction">Direction in the movement plane to rotate towards.</param>
        /// <param name="maxDegreesMainAxis">Maximum number of degrees to rotate this frame around the character's main axis. This is rotating left and right as a character normally does.</param>
        /// <param name="maxDegreesOffAxis">Maximum number of degrees to rotate this frame around other axes. This is used to ensure the character's up direction is correct.
        /// 		It is only used for non-planar worlds where the up direction changes depending on the position of the character.
        ///      More precisely a faster code path which ignores this parameter is used whenever the current #movementPlane is exactly the XZ or XY plane.
        /// 		This must be at least as large as maxDegreesMainAxis.</param>
        protected Quaternion SimulateRotationTowards (Vector2 direction, float maxDegreesMainAxis, float maxDegreesOffAxis = float.PositiveInfinity) {
            return default;
        }

        /// <summary>\copydocref{IAstarAI.Move}</summary>
        public virtual void Move (Vector3 deltaPosition) {
        }

        /// <summary>
        /// Moves the agent to a position.
        ///
        /// This is used if you want to override how the agent moves. For example if you are using
        /// root motion with Mecanim.
        ///
        /// This will use a CharacterController, Rigidbody, Rigidbody2D or the Transform component depending on what options
        /// are available.
        ///
        /// The agent will be clamped to the navmesh after the movement (if such information is available, generally this is only done by the RichAI component).
        ///
        /// See: <see cref="MovementUpdate"/> for some example code.
        /// See: <see cref="controller"/>, <see cref="rigid"/>, <see cref="rigid2D"/>
        /// </summary>
        /// <param name="nextPosition">New position of the agent.</param>
        /// <param name="nextRotation">New rotation of the agent. If #enableRotation is false then this parameter will be ignored.</param>
        public virtual void FinalizeMovement(Vector3 nextPosition, Quaternion nextRotation, bool clampToNavMesh = true, bool updateOnlyGravity = false) {
        }

        void FinalizeRotation (Quaternion nextRotation) {
        }

        void FinalizePosition (Vector3 nextPosition, bool clampToNavMesh, bool updateOnlyGravity) {
        }

        protected void UpdateVelocity()
        {
        }

        /// <summary>
        /// Constrains the character's position to lie on the navmesh.
        /// Not all movement scripts have support for this.
        ///
        /// Returns: New position of the character that has been clamped to the navmesh.
        /// </summary>
        /// <param name="position">Current position of the character.</param>
        /// <param name="positionChanged">True if the character's position was modified by this method.</param>
        protected virtual Vector3 ClampToNavmesh(Vector3 position, out bool positionChanged)
        {
            positionChanged = default(bool);
            return default;
        }

        /// <summary>
        /// Hit info from the last raycast done for ground placement.
        /// Will not update unless gravity is used (if no gravity is used, then raycasts are disabled).
        ///
        /// See: <see cref="RaycastPosition"/>
        /// </summary>
        protected RaycastHit lastRaycastHit;

		/// <summary>
		/// Checks if the character is grounded and prevents ground penetration.
		///
		/// Sets <see cref="verticalVelocity"/> to zero if the character is grounded.
		///
		/// Returns: The new position of the character.
		/// </summary>
		/// <param name="position">Position of the character in the world.</param>
		/// <param name="lastElevation">Elevation coordinate before the agent was moved. This is along the 'up' axis of the #movementPlane.</param>
		protected Vector3 RaycastPosition (Vector3 position, float lastElevation, out bool grounded) {
            grounded = default(bool);
            return default;
        }

        protected virtual void OnDrawGizmosSelected()
        {
        }

        public static readonly Color ShapeGizmoColor = new Color(240/255f, 213/255f, 30/255f);
		public static void PauseAll() {
        }

        public static void UnpauseAll() {
        }

        public void Pause()
        {
        }

        public void Unpause()
        {
        }

        public override void DrawGizmos()
        {
        }

        protected override void Reset()
        {
        }

        void ResetShape () {
        }

        protected override void OnUpgradeSerializedData (ref Serialization.Migrations migrations, bool unityThread) {
        }
    }
}
