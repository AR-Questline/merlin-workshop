using UnityEngine;
using Unity.Mathematics;
using Pathfinding.Pooling;

namespace Pathfinding {
	/// <summary>
	/// Finds a path in a random direction from the start node.
	///
	/// Terminates and returns when G \>= length (passed to the constructor) + RandomPath.spread or when there are no more nodes left to search.
	///
	/// <code>
	///
	/// // Call a RandomPath call like this, assumes that a Seeker is attached to the GameObject
	///
	/// // The path will be returned when the path is over a specified length (or more accurately when the traversal cost is greater than a specified value).
	/// // A score of 1000 is approximately equal to the cost of moving one world unit.
	/// int theGScoreToStopAt = 50000;
	///
	/// // Create a path object
	/// RandomPath path = RandomPath.Construct(transform.position, theGScoreToStopAt);
	/// // Determines the variation in path length that is allowed
	/// path.spread = 5000;
	///
	/// // Get the Seeker component which must be attached to this GameObject
	/// Seeker seeker = GetComponent<Seeker>();
	///
	/// // Start the path and return the result to MyCompleteFunction (which is a function you have to define, the name can of course be changed)
	/// seeker.StartPath(path, MyCompleteFunction);
	///
	/// </code>
	///
	/// [Open online documentation to see videos]
	///
	/// See: wander (view in online documentation for working links)
	/// </summary>
	public class RandomPath : ABPath {
		/// <summary>
		/// G score to stop searching at.
		/// The G score is rougly the distance to get from the start node to a node multiplied by 1000 (per default, see Pathfinding.Int3.Precision), plus any penalties.
		///
		/// [Open online documentation to see videos]
		/// </summary>
		public int searchLength;

		/// <summary>
		/// All G scores between <see cref="searchLength"/> and <see cref="searchLength"/>+<see cref="spread"/> are valid end points, a random one of them is chosen as the final point.
		/// On grid graphs a low spread usually works (but keep it higher than nodeSize*1000 since that it the default cost of moving between two nodes), on NavMesh graphs
		/// I would recommend a higher spread so it can evaluate more nodes.
		///
		/// [Open online documentation to see videos]
		/// </summary>
		public int spread = 5000;

		/// <summary>
		/// If an <see cref="aim"/> is set, the higher this value is, the more it will try to reach <see cref="aim"/>.
		///
		/// Recommended values are between 0 and 10.
		/// </summary>
		public float aimStrength;

		/// <summary>Currently chosen end node</summary>
		uint chosenPathNodeIndex;
		uint chosenPathNodeGScore;

		/// <summary>
		/// The node with the highest G score which is still lower than <see cref="searchLength"/>.
		/// Used as a backup if a node with a G score higher than <see cref="searchLength"/> could not be found
		/// </summary>
		uint maxGScorePathNodeIndex;

		/// <summary>The G score of <see cref="maxGScorePathNodeIndex"/></summary>
		uint maxGScore;

		/// <summary>
		/// An aim can be used to guide the pathfinder to not take totally random paths.
		/// For example you might want your AI to continue in generally the same direction as before, then you can specify
		/// aim to be transform.postion + transform.forward*10 which will make it more often take paths nearer that point
		/// See: <see cref="aimStrength"/>
		/// </summary>
		public Vector3 aim;

		int nodesEvaluatedRep;

		/// <summary>Random number generator</summary>
		readonly System.Random rnd = new System.Random();

		protected override bool hasEndPoint => false;

		public override bool endPointKnownBeforeCalculation => false;

		protected override void Reset () {
        }

        public RandomPath()
        {
        }

        public static RandomPath Construct(Vector3 start, int length, OnPathDelegate callback = null)
        {
            return default;
        }

        protected RandomPath Setup(Vector3 start, int length, OnPathDelegate callback)
        {
            return default;
        }

        /// <summary>
        /// Calls callback to return the calculated path.
        /// See: <see cref="callback"/>
        /// </summary>
        protected override void ReturnPath()
        {
        }

        protected override void Prepare()
        {
        }

        protected override void OnHeapExhausted()
        {
        }

        protected override void OnFoundEndNode(uint pathNode, uint hScore, uint gScore)
        {
        }

        public override void OnVisitNode(uint pathNode, uint hScore, uint gScore)
        {
        }
    }
}
