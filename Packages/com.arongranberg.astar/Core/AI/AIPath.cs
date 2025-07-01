using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Drawing;

	/// <summary>
	/// AI for following paths.
	///
	/// This AI is the default movement script which comes with the A* Pathfinding Project.
	/// It is in no way required by the rest of the system, so feel free to write your own. But I hope this script will make it easier
	/// to set up movement for the characters in your game.
	/// This script works well for many types of units, but if you need the highest performance (for example if you are moving hundreds of characters) you
	/// may want to customize this script or write a custom movement script to be able to optimize it specifically for your game.
	///
	/// This script will try to move to a given <see cref="destination"/>. At <see cref="Pathfinding.AutoRepathPolicy.period;regular intervals"/>, the path to the destination will be recalculated.
	/// If you want to make the AI to follow a particular object you can attach the <see cref="Pathfinding.AIDestinationSetter"/> component.
	/// Take a look at the getstarted (view in online documentation for working links) tutorial for more instructions on how to configure this script.
	///
	/// Here is a video of this script being used move an agent around (it also uses the <see cref="MineBotAnimation"/> component to drive the animations):
	/// [Open online documentation to see videos]
	///
	/// \section variables Quick overview of the variables
	/// In the inspector in Unity, you will see a bunch of variables. You can view detailed information further down, but here's a quick overview.
	///
	/// The <see cref="Pathfinding.AutoRepathPolicy.period;period setting"/> determines how often it will search for new paths, if you have fast moving targets, you might want to set it to a lower value.
	/// The <see cref="destination"/> field is where the AI will try to move, it can be a point on the ground where the player has clicked in an RTS for example.
	/// Or it can be the player object in a zombie game.
	/// The <see cref="maxSpeed"/> is self-explanatory, as is <see cref="rotationSpeed"/>. however <see cref="slowdownDistance"/> might require some explanation:
	/// It is the approximate distance from the target where the AI will start to slow down. Setting it to a large value will make the AI slow down very gradually.
	/// <see cref="pickNextWaypointDist"/> determines the distance to the point the AI will move to (see image below).
	///
	/// Below is an image illustrating several variables that are exposed by this class (<see cref="pickNextWaypointDist"/>, <see cref="steeringTarget"/>, <see cref="desiredVelocity)"/>
	/// [Open online documentation to see images]
	///
	/// This script has many movement fallbacks.
	/// If it finds an RVOController attached to the same GameObject as this component, it will use that. If it finds a character controller it will also use that.
	/// If it finds a rigidbody it will use that. Lastly it will fall back to simply modifying Transform.position which is guaranteed to always work and is also the most performant option.
	///
	/// \section how-aipath-works How it works
	/// In this section I'm going to go over how this script is structured and how information flows.
	/// This is useful if you want to make changes to this script or if you just want to understand how it works a bit more deeply.
	/// However you do not need to read this section if you are just going to use the script as-is.
	///
	/// This script inherits from the <see cref="AIBase"/> class. The movement happens either in Unity's standard Update or FixedUpdate method.
	/// They are both defined in the AIBase class. Which one is actually used depends on if a rigidbody is used for movement or not.
	/// Rigidbody movement has to be done inside the FixedUpdate method while otherwise it is better to do it in Update.
	///
	/// From there a call is made to the <see cref="MovementUpdate"/> method (which in turn calls <see cref="MovementUpdateInternal)"/>.
	/// This method contains the main bulk of the code and calculates how the AI *wants* to move. However it doesn't do any movement itself.
	/// Instead it returns the position and rotation it wants the AI to move to have at the end of the frame.
	/// The Update (or FixedUpdate) method then passes these values to the <see cref="FinalizeMovement"/> method which is responsible for actually moving the character.
	/// That method also handles things like making sure the AI doesn't fall through the ground using raycasting.
	///
	/// The AI recalculates its path regularly. This happens in the Update method which checks <see cref="shouldRecalculatePath"/>, and if that returns true it will call <see cref="SearchPath"/>.
	/// The <see cref="SearchPath"/> method will prepare a path request and send it to the <see cref="Seeker"/> component, which should be attached to the same GameObject as this script.
	/// </summary>
	[AddComponentMenu("Pathfinding/AI/AIPath (2D,3D)")]
	[UniqueComponent(tag = "ai")]
	[DisallowMultipleComponent]
	public partial class AIPath : AIBase, IAstarAI {
		/// <summary>
		/// How quickly the agent accelerates.
		/// Positive values represent an acceleration in world units per second squared.
		/// Negative values are interpreted as an inverse time of how long it should take for the agent to reach its max speed.
		/// For example if it should take roughly 0.4 seconds for the agent to reach its max speed then this field should be set to -1/0.4 = -2.5.
		/// For a negative value the final acceleration will be: -acceleration*maxSpeed.
		/// This behaviour exists mostly for compatibility reasons.
		///
		/// In the Unity inspector there are two modes: Default and Custom. In the Default mode this field is set to -2.5 which means that it takes about 0.4 seconds for the agent to reach its top speed.
		/// In the Custom mode you can set the acceleration to any positive value.
		/// </summary>
		public float maxAcceleration = -2.5f;

		/// <summary>
		/// Rotation speed in degrees per second.
		/// Rotation is calculated using Quaternion.RotateTowards. This variable represents the rotation speed in degrees per second.
		/// The higher it is, the faster the character will be able to rotate.
		/// </summary>
		[UnityEngine.Serialization.FormerlySerializedAs("turningSpeed")]
		public float rotationSpeed = 360;

		/// <summary>Distance from the end of the path where the AI will start to slow down</summary>
		public float slowdownDistance = 0.6F;

		/// <summary>
		/// How far the AI looks ahead along the path to determine the point it moves to.
		/// In world units.
		/// If you enable the <see cref="alwaysDrawGizmos"/> toggle this value will be visualized in the scene view as a blue circle around the agent.
		/// [Open online documentation to see images]
		///
		/// Here are a few example videos showing some typical outcomes with good values as well as how it looks when this value is too low and too high.
		/// <table>
		/// <tr><td>[Open online documentation to see videos]</td><td>\xmlonly <verbatim><span class="label label-danger">Too low</span><br/></verbatim>\endxmlonly A too low value and a too low acceleration will result in the agent overshooting a lot and not managing to follow the path well.</td></tr>
		/// <tr><td>[Open online documentation to see videos]</td><td>\xmlonly <verbatim><span class="label label-warning">Ok</span><br/></verbatim>\endxmlonly A low value but a high acceleration works decently to make the AI follow the path more closely. Note that the <see cref="Pathfinding.AILerp"/> component is better suited if you want the agent to follow the path without any deviations.</td></tr>
		/// <tr><td>[Open online documentation to see videos]</td><td>\xmlonly <verbatim><span class="label label-success">Ok</span><br/></verbatim>\endxmlonly A reasonable value in this example.</td></tr>
		/// <tr><td>[Open online documentation to see videos]</td><td>\xmlonly <verbatim><span class="label label-success">Ok</span><br/></verbatim>\endxmlonly A reasonable value in this example, but the path is followed slightly more loosely than in the previous video.</td></tr>
		/// <tr><td>[Open online documentation to see videos]</td><td>\xmlonly <verbatim><span class="label label-danger">Too high</span><br/></verbatim>\endxmlonly A too high value will make the agent follow the path too loosely and may cause it to try to move through obstacles.</td></tr>
		/// </table>
		/// </summary>
		public float pickNextWaypointDist = 2;

		/// <summary>Draws detailed gizmos constantly in the scene view instead of only when the agent is selected and settings are being modified</summary>
		public bool alwaysDrawGizmos;

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
		/// Ensure that the character is always on the traversable surface of the navmesh.
		/// When this option is enabled a <see cref="AstarPath.GetNearest"/> query will be done every frame to find the closest node that the agent can walk on
		/// and if the agent is not inside that node, then the agent will be moved to it.
		///
		/// This is especially useful together with local avoidance in order to avoid agents pushing each other into walls.
		/// See: local-avoidance (view in online documentation for working links) for more info about this.
		///
		/// This option also integrates with local avoidance so that if the agent is say forced into a wall by other agents the local avoidance
		/// system will be informed about that wall and can take that into account.
		///
		/// Enabling this has some performance impact depending on the graph type (pretty fast for grid graphs, slightly slower for navmesh/recast graphs).
		/// If you are using a navmesh/recast graph you may want to switch to the <see cref="Pathfinding.RichAI"/> movement script which is specifically written for navmesh/recast graphs and
		/// does this kind of clamping out of the box. In many cases it can also follow the path more smoothly around sharp bends in the path.
		///
		/// It is not recommended that you use this option together with the funnel modifier on grid graphs because the funnel modifier will make the path
		/// go very close to the border of the graph and this script has a tendency to try to cut corners a bit. This may cause it to try to go slightly outside the
		/// traversable surface near corners and that will look bad if this option is enabled.
		///
		/// Warning: This option makes no sense to use on point graphs because point graphs do not have a surface.
		/// Enabling this option when using a point graph will lead to the agent being snapped to the closest node every frame which is likely not what you want.
		///
		/// Below you can see an image where several agents using local avoidance were ordered to go to the same point in a corner.
		/// When not constraining the agents to the graph they are easily pushed inside obstacles.
		/// [Open online documentation to see images]
		/// </summary>
		public bool constrainInsideGraph = false;

		/// <summary>Current path which is followed</summary>
		protected Path path;

		/// <summary>Represents the current steering target for the agent</summary>
		protected PathInterpolator.Cursor interpolator;
		/// <summary>Helper which calculates points along the current path</summary>
		protected PathInterpolator interpolatorPath = new PathInterpolator();

		#region IAstarAI implementation

		/// <summary>\copydoc Pathfinding::IAstarAI::Teleport</summary>
		public override void Teleport (Vector3 newPosition, bool clearPath = true) {
        }

        /// <summary>\copydoc Pathfinding::IAstarAI::remainingDistance</summary>
        public float remainingDistance => interpolator.valid ? interpolator.remainingDistance + movementPlane.ToPlane(interpolator.position - position).magnitude : float.PositiveInfinity;

		/// <summary>\copydoc Pathfinding::IAstarAI::reachedDestination</summary>
		public override bool reachedDestination {
			get {
				if (!reachedEndOfPath) return false;
				if (!interpolator.valid || remainingDistance + movementPlane.ToPlane(destination - interpolator.endPoint).magnitude > endReachedDistance) return false;

				// Don't do height checks in 2D mode
				if (orientation != OrientationMode.YAxisForward) {
					// Check if the destination is above the head of the character or far below the feet of it
					movementPlane.ToPlane(destination - position, out float yDifference);
					var h = tr.localScale.y * height;
					if (yDifference > h || yDifference < -h*0.5) return false;
				}

				return true;
			}
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::reachedEndOfPath</summary>
		public bool reachedEndOfPath { get; protected set; }

		/// <summary>\copydoc Pathfinding::IAstarAI::hasPath</summary>
		public bool hasPath => interpolator.valid;

		/// <summary>\copydoc Pathfinding::IAstarAI::pathPending</summary>
		public bool pathPending => waitingForPathCalculation;

		/// <summary>\copydoc Pathfinding::IAstarAI::steeringTarget</summary>
		public Vector3 steeringTarget => interpolator.valid ? interpolator.position : position;

		/// <summary>\copydoc Pathfinding::IAstarAI::endOfPath</summary>
		public override Vector3 endOfPath {
			get {
				if (interpolator.valid) return interpolator.endPoint;
				if (float.IsFinite(destination.x)) return destination;
				return position;
			}
		}

		/// <summary>\copydoc Pathfinding::IAstarAI::radius</summary>
		float IAstarAI.radius { get => radius; set => radius = value; }

		/// <summary>\copydoc Pathfinding::IAstarAI::height</summary>
		float IAstarAI.height { get => height; set => height = value; }

		/// <summary>\copydoc Pathfinding::IAstarAI::maxSpeed</summary>
		float IAstarAI.maxSpeed { get => maxSpeed; set => maxSpeed = value; }

		/// <summary>\copydoc Pathfinding::IAstarAI::canSearch</summary>
		bool IAstarAI.canSearch { get => canSearch; set => canSearch = value; }

		/// <summary>\copydoc Pathfinding::IAstarAI::canMove</summary>
		bool IAstarAI.canMove { get => canMove; set => canMove = value; }

		/// <summary>\copydoc Pathfinding::IAstarAI::movementPlane</summary>
		NativeMovementPlane IAstarAI.movementPlane => new NativeMovementPlane(movementPlane);

		#endregion

		/// <summary>\copydocref{IAstarAI.GetRemainingPath(List<Vector3>,bool)}</summary>
		public void GetRemainingPath (List<Vector3> buffer, out bool stale) {
            stale = default(bool);
        }

        /// <summary>\copydocref{IAstarAI.GetRemainingPath(List<Vector3>,List<PathPartWithLinkInfo>,bool)}</summary>
        public void GetRemainingPath (List<Vector3> buffer, List<PathPartWithLinkInfo> partsBuffer, out bool stale) {
            stale = default(bool);
        }

        protected override void OnDisable () {
        }

        /// <summary>
        /// The end of the path has been reached.
        /// If you want custom logic for when the AI has reached it's destination add it here. You can
        /// also create a new script which inherits from this one and override the function in that script.
        ///
        /// This method will be called again if a new path is calculated as the destination may have changed.
        /// So when the agent is close to the destination this method will typically be called every <see cref="Pathfinding.AutoRepathPolicy.period;period"/> seconds.
        ///
        /// Deprecated: Avoid overriding this method. Instead poll the <see cref="reachedDestination"/> or <see cref="reachedEndOfPath"/> properties.
        /// </summary>
        public virtual void OnTargetReached () {
        }

        protected virtual void UpdateMovementPlane () {
        }

        /// <summary>
        /// Called when a requested path has been calculated.
        /// A path is first requested by <see cref="SearchPath"/>, it is then calculated, probably in the same or the next frame.
        /// Finally it is returned to the seeker which forwards it to this function.
        /// </summary>
        protected override void OnPathComplete (Path newPath) {
        }

        protected override void ClearPath()
        {
        }

        /// <summary>Called during either Update or FixedUpdate depending on if rigidbodies are used for movement or not</summary>
        protected override void MovementUpdateInternal(float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation)
        {
            nextPosition = default(Vector3);
            nextRotation = default(Quaternion);
        }

        Vector2 rotationFilterState, rotationFilterState2;

        protected virtual void CalculateNextRotation(float slowdown, bool avoidingOtherAgents, out Quaternion nextRotation)
        {
            nextRotation = default(Quaternion);
        }

        static NNConstraint cachedNNConstraint = NNConstraint.Walkable;
        protected override Vector3 ClampToNavmesh(Vector3 position, out bool positionChanged)
        {
            positionChanged = default(bool);
            return default;
        }

#if UNITY_EDITOR
        [System.NonSerialized]
        int gizmoHash = 0;

        [System.NonSerialized]
        float lastChangedTime = float.NegativeInfinity;

        protected static readonly Color GizmoColor = new Color(46.0f / 255, 104.0f / 255, 201.0f / 255);

        public override void DrawGizmos()
        {
        }
#endif

        protected override void OnUpgradeSerializedData(ref Serialization.Migrations migrations, bool unityThread)
        {
        }
    }
}
