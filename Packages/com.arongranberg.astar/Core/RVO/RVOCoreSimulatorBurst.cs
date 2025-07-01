using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>Local avoidance related classes</summary>
namespace Pathfinding.RVO {
	using System;
	using Pathfinding.Jobs;
	using Pathfinding.Drawing;
	using Pathfinding.Util;
	using Pathfinding.Sync;
	using Pathfinding.ECS.RVO;
	using Pathfinding.Collections;

	public interface IMovementPlaneWrapper {
		float2 ToPlane(float3 p);
		float2 ToPlane(float3 p, out float elevation);
		float3 ToWorld(float2 p, float elevation = 0);
		Bounds ToWorld(Bounds bounds);

		/// <summary>Maps from 2D (X, Y, 0) coordinates to world coordinates</summary>
		float4x4 matrix { get; }
		void Set(NativeMovementPlane plane);
	}

	public struct XYMovementPlane : IMovementPlaneWrapper {
		public float2 ToPlane(float3 p) => p.xy;
		public float2 ToPlane (float3 p, out float elevation) {
            elevation = default(float);
            return default;
        }

        public float3 ToWorld(float2 p, float elevation = 0) => new float3(p.x, p.y, elevation);
		public Bounds ToWorld (Bounds bounds) {
            return default;
        }

        public float4x4 matrix
        {
            get
            {
                return float4x4.identity;
            }
        }
        public void Set(NativeMovementPlane plane)
        {
        }
    }

	public struct XZMovementPlane : IMovementPlaneWrapper {
		public float2 ToPlane(float3 p) => p.xz;
		public float2 ToPlane (float3 p, out float elevation) {
            elevation = default(float);
            return default;
        }

        public float3 ToWorld(float2 p, float elevation = 0) => new float3(p.x, elevation, p.y);
		public Bounds ToWorld(Bounds bounds) => bounds;
		public void Set (NativeMovementPlane plane) {
        }

        public float4x4 matrix => float4x4.RotateX(math.radians(90));
	}

	public struct ArbitraryMovementPlane : IMovementPlaneWrapper {
		NativeMovementPlane plane;

		public float2 ToPlane(float3 p) => plane.ToPlane(p);
		public float2 ToPlane(float3 p, out float elevation) => plane.ToPlane(p, out elevation);
		public float3 ToWorld(float2 p, float elevation = 0) => plane.ToWorld(p, elevation);
		public Bounds ToWorld(Bounds bounds) => plane.ToWorld(bounds);
		public void Set (NativeMovementPlane plane) {
        }

        public float4x4 matrix {
			get {
				return math.mul(float4x4.TRS(0, plane.rotation, 1), new float4x4(
					new float4(1, 0, 0, 0),
					new float4(0, 0, 1, 0),
					new float4(0, 1, 0, 0),
					new float4(0, 0, 0, 1)
					));
			}
		}
	}

	[System.Flags]
	public enum AgentDebugFlags : byte {
		Nothing = 0,
		ObstacleVOs = 1 << 0,
		AgentVOs = 1 << 1,
		ReachedState = 1 << 2,
		DesiredVelocity = 1 << 3,
		ChosenVelocity = 1 << 4,
		Obstacles = 1 << 5,
		ForwardClearance = 1 << 6,
	}

	/// <summary>
	/// Exposes properties of an Agent class.
	///
	/// See: RVOController
	/// See: RVOSimulator
	/// </summary>
	public interface IAgent {
		/// <summary>
		/// Internal index of the agent.
		/// See: <see cref="Pathfinding.RVO.SimulatorBurst.simulationData"/>
		/// </summary>
		int AgentIndex { get; }

		/// <summary>
		/// Position of the agent.
		/// The agent does not move by itself, a movement script has to be responsible for
		/// reading the CalculatedTargetPoint and CalculatedSpeed properties and move towards that point with that speed.
		/// This property should ideally be set every frame.
		/// </summary>
		Vector3 Position { get; set; }

		/// <summary>
		/// Optimal point to move towards to avoid collisions.
		/// The movement script should move towards this point with a speed of <see cref="CalculatedSpeed"/>.
		///
		/// See: RVOController.CalculateMovementDelta.
		/// </summary>
		Vector3 CalculatedTargetPoint { get; }

		/// <summary>
		/// True if the agent's movement is affected by any other agents or obstacles.
		///
		/// If the agent is all alone, and can just move in a straight line to its target, this will be false.
		/// If it has to adjust its velocity, even slightly, to avoid collisions, this will be true.
		/// </summary>
		bool AvoidingAnyAgents { get; }

		/// <summary>
		/// Optimal speed of the agent to avoid collisions.
		/// The movement script should move towards <see cref="CalculatedTargetPoint"/> with this speed.
		/// </summary>
		float CalculatedSpeed { get; }

		/// <summary>
		/// Point towards which the agent should move.
		/// Usually you set this once per frame. The agent will try move as close to the target point as possible.
		/// Will take effect at the next simulation step.
		///
		/// Note: The system assumes that the agent will stop when it reaches the target point
		/// so if you just want to move the agent in a particular direction, make sure that you set the target point
		/// a good distance in front of the character as otherwise the system may not avoid colisions that well.
		/// What would happen is that the system (in simplified terms) would think that the agents would stop
		/// before the collision and thus it wouldn't slow down or change course. See the image below.
		/// In the image the desiredSpeed is the length of the blue arrow and the target point
		/// is the point where the black arrows point to.
		/// In the upper case the agent does not avoid the red agent (you can assume that the red
		/// agent has a very small velocity for simplicity) while in the lower case it does.
		/// If you are following a path a good way to pick the target point is to set it to
		/// <code>
		/// targetPoint = directionToNextWaypoint.normalized * remainingPathDistance
		/// </code>
		/// Where remainingPathDistance is the distance until the character would reach the end of the path.
		/// This works well because at the end of the path the direction to the next waypoint will just be the
		/// direction to the last point on the path and remainingPathDistance will be the distance to the last point
		/// in the path, so targetPoint will be set to simply the last point in the path. However when remainingPathDistance
		/// is large the target point will be so far away that the agent will essentially be told to move in a particular
		/// direction, which is precisely what we want.
		/// [Open online documentation to see images]
		/// </summary>
		/// <param name="targetPoint">Target point in world space.</param>
		/// <param name="desiredSpeed">Desired speed of the agent. In world units per second. The agent will try to move with this
		///      speed if possible.</param>
		/// <param name="maxSpeed">Max speed of the agent. In world units per second. If necessary (for example if another agent
		///      is on a collision trajectory towards this agent) the agent can move at this speed.
		///      Should be at least as high as desiredSpeed, but it is recommended to use a slightly
		///      higher value than desiredSpeed (for example desiredSpeed*1.2).</param>
		/// <param name="endOfPath">Point in world space which is the agent's final desired destination on the navmesh.
		/// 	This is typically the end of the path the agent is following.
		/// 	May be set to (+inf,+inf,+inf) to mark the agent as not having a well defined end of path.
		/// 	If this is set, multiple agents with roughly the same end of path will crowd more naturally around this point.
		/// 	They will be able to realize that they cannot get closer if there are many agents trying to get closer to the same destination and then stop.</param>
		void SetTarget(Vector3 targetPoint, float desiredSpeed, float maxSpeed, Vector3 endOfPath);

		/// <summary>
		/// Plane in which the agent moves.
		/// Local avoidance calculations are always done in 2D and this plane determines how to convert from 3D to 2D.
		///
		/// In a typical 3D game the agents move in the XZ plane and in a 2D game they move in the XY plane.
		/// By default this is set to the XZ plane.
		///
		/// See: <see cref="Pathfinding.Util.GraphTransform.xyPlane"/>
		/// See: <see cref="Pathfinding.Util.GraphTransform.xzPlane"/>
		/// </summary>
		Util.SimpleMovementPlane MovementPlane { get; set; }

		/// <summary>Locked agents will be assumed not to move</summary>
		bool Locked { get; set; }

		/// <summary>
		/// Radius of the agent in world units.
		/// Agents are modelled as circles/cylinders.
		/// </summary>
		float Radius { get; set; }

		/// <summary>
		/// Height of the agent in world units.
		/// Agents are modelled as circles/cylinders.
		/// </summary>
		float Height { get; set; }

		/// <summary>
		/// Max number of estimated seconds to look into the future for collisions with agents.
		/// As it turns out, this variable is also very good for controling agent avoidance priorities.
		/// Agents with lower values will avoid other agents less and thus you can make 'high priority agents' by
		/// giving them a lower value.
		/// </summary>
		float AgentTimeHorizon { get; set; }

		/// <summary>Max number of estimated seconds to look into the future for collisions with obstacles</summary>
		float ObstacleTimeHorizon { get; set; }

		/// <summary>
		/// Max number of agents to take into account.
		/// Decreasing this value can lead to better performance, increasing it can lead to better quality of the simulation.
		/// </summary>
		int MaxNeighbours { get; set; }

		/// <summary>Number of neighbours that the agent took into account during the last simulation step</summary>
		int NeighbourCount { get; }

		/// <summary>
		/// Specifies the avoidance layer for this agent.
		/// The <see cref="CollidesWith"/> mask on other agents will determine if they will avoid this agent.
		/// </summary>
		RVOLayer Layer { get; set; }

		/// <summary>
		/// Layer mask specifying which layers this agent will avoid.
		/// You can set it as CollidesWith = RVOLayer.DefaultAgent | RVOLayer.Layer3 | RVOLayer.Layer6 ...
		///
		/// See: http://en.wikipedia.org/wiki/Mask_(computing)
		/// See: bitmasks (view in online documentation for working links)
		/// </summary>
		RVOLayer CollidesWith { get; set; }

		/// <summary>
		/// Determines how strongly this agent just follows the flow instead of making other agents avoid it.
		/// The default value is 0, if it is greater than zero (up to the maximum value of 1) other agents will
		/// not avoid this character as much. However it works in a different way to <see cref="Priority"/>.
		///
		/// A group of agents with FlowFollowingStrength set to a high value that all try to reach the same point
		/// will end up just settling to stationary positions around that point, none will push the others away to any significant extent.
		/// This is tricky to achieve with priorities as priorities are all relative, so setting all agents to a low priority is the same thing
		/// as not changing priorities at all.
		///
		/// Should be a value in the range [0, 1].
		///
		/// TODO: Add video
		/// </summary>
		float FlowFollowingStrength { get; set; }

		/// <summary>Draw debug information in the scene view</summary>
		AgentDebugFlags DebugFlags { get; set; }

		/// <summary>
		/// How strongly other agents will avoid this agent.
		/// Usually a value between 0 and 1.
		/// Agents with similar priorities will avoid each other with an equal strength.
		/// If an agent sees another agent with a higher priority than itself it will avoid that agent more strongly.
		/// In the extreme case (e.g this agent has a priority of 0 and the other agent has a priority of 1) it will treat the other agent as being a moving obstacle.
		/// Similarly if an agent sees another agent with a lower priority than itself it will avoid that agent less.
		///
		/// In general the avoidance strength for this agent is:
		/// <code>
		/// if this.priority > 0 or other.priority > 0:
		///     avoidanceStrength = other.priority / (this.priority + other.priority);
		/// else:
		///     avoidanceStrength = 0.5
		/// </code>
		/// </summary>
		float Priority { get; set; }

		int HierarchicalNodeIndex { get; set; }

		/// <summary>
		/// Callback which will be called right before avoidance calculations are started.
		/// Used to update the other properties with the most up to date values
		/// </summary>
		System.Action PreCalculationCallback { set; }

		/// <summary>
		/// Callback which will be called right the agent is removed from the simulation.
		/// This agent should not be used anymore after this callback has been called.
		/// </summary>
		System.Action DestroyedCallback { set; }

		/// <summary>
		/// Set the normal of a wall (or something else) the agent is currently colliding with.
		/// This is used to make the RVO system aware of things like physics or an agent being clamped to the navmesh.
		/// The velocity of this agent that other agents observe will be modified so that there is no component
		/// into the wall. The agent will however not start to avoid the wall, for that you will need to add RVO obstacles.
		///
		/// This value will be cleared after the next simulation step, normally it should be set every frame
		/// when the collision is still happening.
		/// </summary>
		void SetCollisionNormal(Vector3 normal);

		/// <summary>
		/// Set the current velocity of the agent.
		/// This will override the local avoidance input completely.
		/// It is useful if you have a player controlled character and want other agents to avoid it.
		///
		/// Calling this method will mark the agent as being externally controlled for 1 simulation step.
		/// Local avoidance calculations will be skipped for the next simulation step but will be resumed
		/// after that unless this method is called again.
		/// </summary>
		void ForceSetVelocity(Vector3 velocity);

		public ReachedEndOfPath CalculatedEffectivelyReachedDestination { get; }

		/// <summary>
		/// Add obstacles to avoid for this agent.
		///
		/// The obstacles are based on nearby borders of the navmesh.
		/// You should call this method every frame.
		/// </summary>
		/// <param name="sourceNode">The node to start the obstacle search at. This is typically the node the agent is standing on.</param>
		public void SetObstacleQuery(GraphNode sourceNode);
	}

	/// <summary>
	/// Type of obstacle shape.
	/// See: <see cref="ObstacleVertexGroup"/>
	/// </summary>
	public enum ObstacleType {
		/// <summary>A chain of vertices, the first and last segments end at a point</summary>
		Chain,
		/// <summary>A loop of vertices, the last vertex connects back to the first one</summary>
		Loop,
	}

	public struct ObstacleVertexGroup {
		/// <summary>Type of obstacle shape</summary>
		public ObstacleType type;
		/// <summary>Number of vertices that this group consists of</summary>
		public int vertexCount;
		public float3 boundsMn;
		public float3 boundsMx;
	}

	/// <summary>Represents a set of obstacles</summary>
	public struct UnmanagedObstacle {
		/// <summary>The allocation in <see cref="ObstacleData.obstacleVertices"/> which represents all vertices used for these obstacles</summary>
		public int verticesAllocation;
		/// <summary>The allocation in <see cref="ObstacleData.obstacles"/> which represents the obstacle groups</summary>
		public int groupsAllocation;
	}

	// TODO: Change to byte?
	public enum ReachedEndOfPath {
		/// <summary>The agent has no reached the end of its path yet</summary>
		NotReached,
		/// <summary>
		/// The agent will soon reached the end of the path, or be blocked by other agents such that it cannot get closer.
		/// Typically the agent can only move forward for a fraction of a second before it will become blocked.
		/// </summary>
		ReachedSoon,
		/// <summary>
		/// The agent has reached the end of the path, or it is blocked by other agents such that it cannot get closer right now.
		/// If multiple have roughly the same end of path they will end up crowding around that point and all agents in the crowd will get this status.
		/// </summary>
		Reached,
	}

	// TODO: Change to byte?
	/// <summary>Plane which movement is primarily happening in</summary>
	public enum MovementPlane {
		/// <summary>Movement happens primarily in the XZ plane (3D)</summary>
		XZ,
		/// <summary>Movement happens primarily in the XY plane (2D)</summary>
		XY,
		/// <summary>For curved worlds. See: spherical (view in online documentation for working links)</summary>
		Arbitrary,
	}

	// Note: RVOLayer must not be marked with the [System.Flags] attribute because then Unity will show all RVOLayer fields as mask fields
	// which we do not want
	public enum RVOLayer {
		DefaultAgent = 1 << 0,
		DefaultObstacle = 1 << 1,
		Layer2 = 1 << 2,
		Layer3 = 1 << 3,
		Layer4 = 1 << 4,
		Layer5 = 1 << 5,
		Layer6 = 1 << 6,
		Layer7 = 1 << 7,
		Layer8 = 1 << 8,
		Layer9 = 1 << 9,
		Layer10 = 1 << 10,
		Layer11 = 1 << 11,
		Layer12 = 1 << 12,
		Layer13 = 1 << 13,
		Layer14 = 1 << 14,
		Layer15 = 1 << 15,
		Layer16 = 1 << 16,
		Layer17 = 1 << 17,
		Layer18 = 1 << 18,
		Layer19 = 1 << 19,
		Layer20 = 1 << 20,
		Layer21 = 1 << 21,
		Layer22 = 1 << 22,
		Layer23 = 1 << 23,
		Layer24 = 1 << 24,
		Layer25 = 1 << 25,
		Layer26 = 1 << 26,
		Layer27 = 1 << 27,
		Layer28 = 1 << 28,
		Layer29 = 1 << 29,
		Layer30 = 1 << 30
	}

	/// <summary>
	/// Local Avoidance Simulator.
	/// This class handles local avoidance simulation for a number of agents using
	/// Reciprocal Velocity Obstacles (RVO) and Optimal Reciprocal Collision Avoidance (ORCA).
	///
	/// This class will handle calculation of velocities from desired velocities supplied by a script.
	/// It is, however, not responsible for moving any objects in a Unity Scene. For that there are other scripts (see below).
	///
	/// Agents be added and removed at any time.
	///
	/// See: RVOSimulator
	/// See: RVOAgentBurst
	/// See: Pathfinding.RVO.IAgent
	///
	/// You will most likely mostly use the wrapper class <see cref="RVOSimulator"/>.
	/// </summary>
	public class SimulatorBurst {
		/// <summary>
		/// Inverse desired simulation fps.
		/// See: DesiredDeltaTime
		/// </summary>
		private float desiredDeltaTime = 0.05f;

		/// <summary>Number of agents in this simulation</summary>
		int numAgents = 0;

		/// <summary>
		/// Scope for drawing gizmos even on frames during which the simulation is not running.
		/// This is used to draw the obstacles, quadtree and agent debug lines.
		/// </summary>
		Drawing.RedrawScope debugDrawingScope;

		/// <summary>
		/// Quadtree for this simulation.
		/// Used internally by the simulation to perform fast neighbour lookups for each agent.
		/// Please only read from this tree, do not rebuild it since that can interfere with the simulation.
		/// It is rebuilt when necessary.
		///
		/// Warning: Before accessing this, you should call <see cref="LockSimulationDataReadOnly"/> or <see cref="LockSimulationDataReadWrite"/>.
		/// </summary>
		public RVOQuadtreeBurst quadtree;

		public bool drawQuadtree;

		Action[] agentPreCalculationCallbacks = new Action[0];
		Action[] agentDestroyCallbacks = new Action[0];

		Stack<int> freeAgentIndices = new Stack<int>();
		TemporaryAgentData temporaryAgentData;
		HorizonAgentData horizonAgentData;

		/// <summary>
		/// Internal obstacle data.
		/// Normally you will never need to access this directly.
		///
		/// Warning: Before accessing this, you should call <see cref="LockSimulationDataReadOnly"/> or <see cref="LockSimulationDataReadWrite"/>.
		/// </summary>
		public ObstacleData obstacleData;

		/// <summary>
		/// Internal simulation data.
		/// Can be used if you need very high performance access to the agent data.
		/// Normally you would use the SimulatorBurst.Agent class instead (implements the IAgent interface).
		///
		/// Warning: Before accessing this, you should call <see cref="LockSimulationDataReadOnly"/> or <see cref="LockSimulationDataReadWrite"/>.
		/// </summary>
		public AgentData simulationData;

		/// <summary>
		/// Internal simulation data.
		/// Can be used if you need very high performance access to the agent data.
		/// Normally you would use the SimulatorBurst.Agent class instead (implements the IAgent interface).
		///
		/// Warning: Before accessing this, you should call <see cref="LockSimulationDataReadOnly"/> or <see cref="LockSimulationDataReadWrite"/>.
		/// </summary>
		public AgentOutputData outputData;

		public const int MaxNeighbourCount = 50;
		public const int MaxBlockingAgentCount = 7;

		public const int MaxObstacleVertices = 256;

		public struct AgentNeighbourLookup {
			[ReadOnly]
			[NativeDisableParallelForRestriction]
			NativeArray<int> neighbours;

			public AgentNeighbourLookup(NativeArray<int> neighbours) : this()
            {
            }

            /// <summary>Read-only span with all agent indices that a given agent took into account during its last simulation step</summary>
            public UnsafeSpan<int> GetNeighbours(int agentIndex)
            {
                return default;
            }
        }

        /// <summary>
        /// Lookup to find neighbours of a agents.
        ///
        /// Warning: Before accessing this, you should call <see cref="LockSimulationDataReadOnly"/> or <see cref="LockSimulationDataReadWrite"/>.
        /// </summary>
        public AgentNeighbourLookup GetAgentNeighbourLookup()
        {
            return default;
        }

        struct Agent : IAgent {
			public SimulatorBurst simulator;
			public AgentIndex agentIndex;

			public int AgentIndex => agentIndex.Index;
			public Vector3 Position { get => simulator.simulationData.position[AgentIndex]; set => simulator.simulationData.position[AgentIndex] = value; }
			public bool Locked { get => simulator.simulationData.locked[AgentIndex]; set => simulator.simulationData.locked[AgentIndex] = value; }
			public float Radius { get => simulator.simulationData.radius[AgentIndex]; set => simulator.simulationData.radius[AgentIndex] = value; }
			public float Height { get => simulator.simulationData.height[AgentIndex]; set => simulator.simulationData.height[AgentIndex] = value; }
			public float AgentTimeHorizon { get => simulator.simulationData.agentTimeHorizon[AgentIndex]; set => simulator.simulationData.agentTimeHorizon[AgentIndex] = value; }
			public float ObstacleTimeHorizon { get => simulator.simulationData.obstacleTimeHorizon[AgentIndex]; set => simulator.simulationData.obstacleTimeHorizon[AgentIndex] = value; }
			public int MaxNeighbours { get => simulator.simulationData.maxNeighbours[AgentIndex]; set => simulator.simulationData.maxNeighbours[AgentIndex] = value; }
			public RVOLayer Layer { get => simulator.simulationData.layer[AgentIndex]; set => simulator.simulationData.layer[AgentIndex] = value; }
			public RVOLayer CollidesWith { get => simulator.simulationData.collidesWith[AgentIndex]; set => simulator.simulationData.collidesWith[AgentIndex] = value; }
			public float FlowFollowingStrength { get => simulator.simulationData.flowFollowingStrength[AgentIndex]; set => simulator.simulationData.flowFollowingStrength[AgentIndex] = value; }
			public AgentDebugFlags DebugFlags { get => simulator.simulationData.debugFlags[AgentIndex]; set => simulator.simulationData.debugFlags[AgentIndex] = value; }
			public float Priority { get => simulator.simulationData.priority[AgentIndex]; set => simulator.simulationData.priority[AgentIndex] = value; }
			public int HierarchicalNodeIndex { get => simulator.simulationData.hierarchicalNodeIndex[AgentIndex]; set => simulator.simulationData.hierarchicalNodeIndex[AgentIndex] = value; }
			public SimpleMovementPlane MovementPlane { get => new SimpleMovementPlane(simulator.simulationData.movementPlane[AgentIndex].rotation); set => simulator.simulationData.movementPlane[AgentIndex] = new NativeMovementPlane(value); }
			public Action PreCalculationCallback { set => simulator.agentPreCalculationCallbacks[AgentIndex] = value; }
			public Action DestroyedCallback { set => simulator.agentDestroyCallbacks[AgentIndex] = value; }

			public Vector3 CalculatedTargetPoint {
				get {
					simulator.BlockUntilSimulationStepDone();
					return simulator.outputData.targetPoint[AgentIndex];
				}
			}

			public float CalculatedSpeed {
				get {
					simulator.BlockUntilSimulationStepDone();
					return simulator.outputData.speed[AgentIndex];
				}
			}

			public ReachedEndOfPath CalculatedEffectivelyReachedDestination {
				get {
					simulator.BlockUntilSimulationStepDone();
					return simulator.outputData.effectivelyReachedDestination[AgentIndex];
				}
			}

			public int NeighbourCount {
				get {
					simulator.BlockUntilSimulationStepDone();
					return simulator.outputData.numNeighbours[AgentIndex];
				}
			}

			public bool AvoidingAnyAgents {
				get {
					simulator.BlockUntilSimulationStepDone();
					return simulator.outputData.blockedByAgents[AgentIndex*SimulatorBurst.MaxBlockingAgentCount] != -1;
				}
			}

			public void SetObstacleQuery (GraphNode sourceNode) {
            }

            public void SetTarget(Vector3 targetPoint, float desiredSpeed, float maxSpeed, Vector3 endOfPath)
            {
            }

            public void SetCollisionNormal(Vector3 normal)
            {
            }

            public void ForceSetVelocity(Vector3 velocity)
            {
            }
        }

		/// <summary>Holds internal obstacle data for the local avoidance simulation</summary>
		public struct ObstacleData {
			/// <summary>
			/// Groups of vertices representing obstacles.
			/// An obstacle is either a cycle or a chain of vertices
			/// </summary>
			public SlabAllocator<ObstacleVertexGroup> obstacleVertexGroups;
			/// <summary>Vertices of all obstacles</summary>
			public SlabAllocator<float3> obstacleVertices;
			/// <summary>Obstacle sets, each one is represented as a set of obstacle vertex groups</summary>
			public NativeList<UnmanagedObstacle> obstacles;

			public void Init (Allocator allocator) {
            }

            public void Dispose()
            {
            }
        }

		/// <summary>Holds internal agent data for the local avoidance simulation</summary>
		public struct AgentData {
			// Note: All 3D vectors are in world space
			public NativeArray<AgentIndex> version;
			public NativeArray<float> radius;
			public NativeArray<float> height;
			public NativeArray<float> desiredSpeed;
			public NativeArray<float> maxSpeed;
			public NativeArray<float> agentTimeHorizon;
			public NativeArray<float> obstacleTimeHorizon;
			public NativeArray<bool> locked;
			public NativeArray<int> maxNeighbours;
			public NativeArray<RVOLayer> layer;
			public NativeArray<RVOLayer> collidesWith;
			public NativeArray<float> flowFollowingStrength;
			public NativeArray<float3> position;
			public NativeArray<float3> collisionNormal;
			public NativeArray<bool> manuallyControlled;
			public NativeArray<float> priority;
			public NativeArray<AgentDebugFlags> debugFlags;
			public NativeArray<float3> targetPoint;
			/// <summary>x = signed left angle in radians, y = signed right angle in radians (should be greater than x)</summary>
			public NativeArray<float2> allowedVelocityDeviationAngles;
			public NativeArray<NativeMovementPlane> movementPlane;
			public NativeArray<float3> endOfPath;
			/// <summary>Which obstacle data in the <see cref="ObstacleData.obstacles"/> array the agent should use for avoidance</summary>
			public NativeArray<int> agentObstacleMapping;
			public NativeArray<int> hierarchicalNodeIndex;

			public void Realloc (int size, Allocator allocator)
            {
            }

            public void SetTarget(int agentIndex, float3 targetPoint, float desiredSpeed, float maxSpeed, float3 endOfPath)
            {
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public bool HasDebugFlag(int agentIndex, AgentDebugFlags flag) => Unity.Burst.CompilerServices.Hint.Unlikely((debugFlags[agentIndex] & flag) != 0);

            public void Dispose()
            {
            }
        };

        public struct AgentOutputData
        {
            public NativeArray<float3> targetPoint;
            public NativeArray<float> speed;
            public NativeArray<int> numNeighbours;
            [NativeDisableParallelForRestrictionAttribute]
            public NativeArray<int> blockedByAgents;
            public NativeArray<ReachedEndOfPath> effectivelyReachedDestination;
            public NativeArray<float> forwardClearance;

            public void Realloc(int size, Allocator allocator)
            {
            }

            public void Move(int fromIndex, int toIndex)
            {
            }

            public void Dispose()
            {
            }
        };

        public struct HorizonAgentData
        {
            public NativeArray<int> horizonSide;
            public NativeArray<float> horizonMinAngle;
            public NativeArray<float> horizonMaxAngle;

            public void Realloc(int size, Allocator allocator)
            {
            }

            public void Move(int fromIndex, int toIndex)
            {
            }

            public void Dispose()
            {
            }
        }

        public struct TemporaryAgentData
        {
            public NativeArray<float2> desiredTargetPointInVelocitySpace;
            public NativeArray<float3> desiredVelocity;
            public NativeArray<float3> currentVelocity;
            public NativeArray<float2> collisionVelocityOffsets;
            public NativeArray<int> neighbours;

            public void Realloc(int size, Allocator allocator)
            {
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Time in seconds between each simulation step.
        /// This is the desired delta time, the simulation will never run at a higher fps than
        /// the rate at which the Update function is called.
        /// </summary>
        public float DesiredDeltaTime { get { return desiredDeltaTime; } set { desiredDeltaTime = System.Math.Max(value, 0.0f); } }

        /// <summary>
        /// Bias agents to pass each other on the right side.
        /// If the desired velocity of an agent puts it on a collision course with another agent or an obstacle
        /// its desired velocity will be rotated this number of radians (1 radian is approximately 57°) to the right.
        /// This helps to break up symmetries and makes it possible to resolve some situations much faster.
        ///
        /// When many agents have the same goal this can however have the side effect that the group
        /// clustered around the target point may as a whole start to spin around the target point.
        ///
        /// Recommended values are in the range of 0 to 0.2.
        ///
        /// If this value is negative, the agents will be biased towards passing each other on the left side instead.
        /// </summary>
        public float SymmetryBreakingBias { get; set; }

        /// <summary>Use hard collisions</summary>
        public bool HardCollisions { get; set; }

        public bool UseNavmeshAsObstacle { get; set; }

        public Rect AgentBounds
        {
            get
            {
                rwLock.ReadSync().Unlock();
                return quadtree.bounds;
            }
        }

        /// <summary>Number of agents in the simulation</summary>
        public int AgentCount => numAgents;

        public MovementPlane MovementPlane => movementPlane;

        /// <summary>Determines if the XY (2D) or XZ (3D) plane is used for movement</summary>
        public readonly MovementPlane movementPlane = MovementPlane.XZ;

        /// <summary>Used to synchronize access to the simulation data</summary>
        RWLock rwLock = new RWLock();

        public void BlockUntilSimulationStepDone()
        {
        }

        /// <summary>Create a new simulator.</summary>
        /// <param name="movementPlane">The plane that the movement happens in. XZ for 3D games, XY for 2D games.</param>
        public SimulatorBurst(MovementPlane movementPlane)
        {
        }

        /// <summary>Removes all agents from the simulation</summary>
        public void ClearAgents()
        {
        }

        /// <summary>
        /// Frees all used memory.
        /// Warning: You must call this when you are done with the simulator, otherwise some resources can linger and lead to memory leaks.
        /// </summary>
        public void OnDestroy()
        {
        }

        void AllocateAgentSpace()
        {
        }

        /// <summary>
        /// Add an agent at the specified position.
        /// You can use the returned interface to read and write parameters
        /// and set for example radius and desired point to move to.
        ///
        /// See: <see cref="RemoveAgent"/>
        /// </summary>
        /// <param name="position">See \reflink{IAgent.Position}</param>
        public IAgent AddAgent(Vector3 position)
        {
            return default;
        }

        /// <summary>
        /// Add an agent at the specified position.
        /// You can use the returned index to read and write parameters
        /// and set for example radius and desired point to move to.
        ///
        /// See: <see cref="RemoveAgent"/>
        /// </summary>
        public AgentIndex AddAgentBurst(float3 position)
        {
            return default;
        }

        /// <summary>Deprecated: Use AddAgent(Vector3) instead</summary>
        [System.Obsolete("Use AddAgent(Vector3) instead", true)]
        public IAgent AddAgent(IAgent agent)
        {
            return default;
        }

        /// <summary>
        /// Removes a specified agent from this simulation.
        /// The agent can be added again later by using AddAgent.
        ///
        /// See: AddAgent(IAgent)
        /// See: ClearAgents
        /// </summary>
        public void RemoveAgent(IAgent agent)
        {
        }

        public void RemoveAgent(AgentIndex agent)
        {
        }

        void PreCalculation(JobHandle dependency)
        {
        }

        /// <summary>Should be called once per frame.</summary>
        /// <param name="dependency">Jobs that need to complete before local avoidance runs.</param>
        /// <param name="dt">Length of timestep in seconds.</param>
        /// <param name="drawGizmos">If true, debug gizmos will be allowed to render (they never render in standalone games, though).</param>
        /// <param name="allocator">Allocator to use for some temporary allocations. Should be a rewindable allocator since no disposal will be done.</param>
        public JobHandle Update(JobHandle dependency, float dt, bool drawGizmos, Allocator allocator)
        {
            return default;
        }

        /// <summary>
        /// Takes an async read-only lock on the simulation data.
        ///
        /// This can be used to access <see cref="simulationData"/>, <see cref="outputData"/>, <see cref="quadtree"/>, and <see cref="GetAgentNeighbourLookup"/> in a job.
        ///
        /// Use the <see cref="ReadLockAsync.dependency"/> field when you schedule the job using the simulation data,
        /// and then call <see cref="ReadLockAsync.UnlockAfter"/> with the job handle of that job.
        /// </summary>
        public RWLock.ReadLockAsync LockSimulationDataReadOnly()
        {
            return default;
        }

        /// <summary>
        /// Takes an async read/write lock on the simulation data.
        ///
        /// This can be used to access <see cref="simulationData"/>, <see cref="outputData"/>, <see cref="quadtree"/>, and <see cref="GetAgentNeighbourLookup"/> in a job.
        ///
        /// Use the <see cref="WriteLockAsync.dependency"/> field when you schedule the job using the simulation data,
        /// and then call <see cref="WriteLockAsync.UnlockAfter"/> with the job handle of that job.
        /// </summary>
        public RWLock.WriteLockAsync LockSimulationDataReadWrite()
        {
            return default;
        }

        JobHandle UpdateInternal<T>(JobHandle dependency, float deltaTime, bool drawGizmos, Allocator allocator) where T : struct, IMovementPlaneWrapper
        {
            return default;
        }
    }
}
