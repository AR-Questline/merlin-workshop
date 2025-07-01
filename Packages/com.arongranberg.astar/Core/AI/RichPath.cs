using System;
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;
using Pathfinding.Pooling;
using Unity.Collections;
using UnityEngine.Assertions;

namespace Pathfinding {
	public class RichPath {
		int currentPart;
		readonly List<RichPathPart> parts = new List<RichPathPart>();

		public Seeker seeker;

		/// <summary>
		/// Transforms points from path space to world space.
		/// If null the identity transform will be used.
		///
		/// This is used when the world position of the agent does not match the
		/// corresponding position on the graph. This is the case in the example
		/// scene called 'Moving'.
		///
		/// See: <see cref="Pathfinding.Examples.LocalSpaceRichAI"/>
		/// </summary>
		public ITransform transform;

		public RichPath () {
        }

        public void Clear () {
        }

        /// <summary>Use this for initialization.</summary>
        /// <param name="seeker">Optionally provide in order to take tag penalties into account. May be null if you do not use a Seeker\</param>
        /// <param name="path">Path to follow</param>
        /// <param name="mergePartEndpoints">If true, then adjacent parts that the path is split up in will
        /// try to use the same start/end points. For example when using a link on a navmesh graph
        /// Instead of first following the path to the center of the node where the link is and then
        /// follow the link, the path will be adjusted to go to the exact point where the link starts
        /// which usually makes more sense.</param>
        /// <param name="simplificationMode">The path can optionally be simplified. This can be a bit expensive for long paths.</param>
        public void Initialize (Seeker seeker, Path path, bool mergePartEndpoints, bool simplificationMode) {
        }

        public Vector3 Endpoint { get; private set; }

        /// <summary>True if we have completed (called NextPart for) the last part in the path</summary>
        public bool CompletedAllParts
        {
            get
            {
                return currentPart >= parts.Count;
            }
        }

        /// <summary>True if we are traversing the last part of the path</summary>
        public bool IsLastPart
        {
            get
            {
                return currentPart >= parts.Count - 1;
            }
        }

        public void NextPart()
        {
        }

        public RichPathPart GetCurrentPart()
        {
            return default;
        }

        /// <summary>
        /// Replaces the buffer with the remaining path.
        /// See: <see cref="Pathfinding.IAstarAI.GetRemainingPath"/>
        /// </summary>
        public void GetRemainingPath(List<Vector3> buffer, List<PathPartWithLinkInfo> partsBuffer, Vector3 currentPosition, out bool requiresRepath)
        {
            requiresRepath = default(bool);
        }
    }

    public abstract class RichPathPart : IAstarPooledObject
    {
        public abstract void OnEnterPool();
    }

    public class RichFunnel : RichPathPart
    {
        NativeList<Vector3> left;
        NativeList<Vector3> right;
        List<TriangleMeshNode> nodes;
        public Vector3 exactStart;
        public Vector3 exactEnd;
        NavmeshBase graph;
        int currentNode;
        Vector3 currentPosition;
        int checkForDestroyedNodesCounter;
        RichPath path;
        int[] triBuffer = new int[3];

        /// <summary>Post process the funnel corridor or not</summary>
        public bool funnelSimplification = true;

#if UNITY_EDITOR
        static bool s_quitting;

        static RichFunnel()
        {
        }
#endif

        public RichFunnel()
        {
        }

        ~RichFunnel()
        {
        }

        /// <summary>Works like a constructor, but can be used even for pooled objects. Returns this for easy chaining</summary>
        public RichFunnel Initialize(RichPath path, NavmeshBase graph)
        {
            return default;
        }

        public override void OnEnterPool()
        {
        }

        public TriangleMeshNode CurrentNode
        {
            get
            {
                var node = nodes[currentNode];
                if (!node.Destroyed)
                {
                    return node;
                }
                return null;
            }
        }

        /// <summary>
        /// Build a funnel corridor from a node list slice.
        /// The nodes are assumed to be of type TriangleMeshNode.
        /// </summary>
        /// <param name="nodes">Nodes to build the funnel corridor from</param>
        /// <param name="start">Start index in the nodes list</param>
        /// <param name="end">End index in the nodes list, this index is inclusive</param>
        public void BuildFunnelCorridor(List<GraphNode> nodes, int start, int end)
        {
        }

        /// <summary>
        /// Split funnel at node index splitIndex and throw the nodes up to that point away and replace with prefix.
        /// Used when the AI has happened to get sidetracked and entered a node outside the funnel.
        /// </summary>
        void UpdateFunnelCorridor(int splitIndex, List<TriangleMeshNode> prefix)
        {
        }

        /// <summary>True if any node in the path is destroyed</summary>
        bool CheckForDestroyedNodes()
        {
            return default;
        }

        /// <summary>
        /// Approximate distance (as the crow flies) to the endpoint of this path part.
        /// See: <see cref="exactEnd"/>
        /// </summary>
        public float DistanceToEndOfPath
        {
            get
            {
                var currentNode = CurrentNode;
                Vector3 closestOnNode = currentNode != null ? currentNode.ClosestPointOnNode(currentPosition) : currentPosition;
                return (exactEnd - closestOnNode).magnitude;
            }
        }

        /// <summary>
        /// Clamps the position to the navmesh and repairs the path if the agent has moved slightly outside it.
        /// You should not call this method with anything other than the agent's position.
        /// </summary>
        public Vector3 ClampToNavmesh(Vector3 position)
        {
            return default;
        }

        /// <summary>
        /// Find the next points to move towards and clamp the position to the navmesh.
        ///
        /// Returns: The position of the agent clamped to make sure it is inside the navmesh.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="buffer">Will be filled with up to numCorners points which are the next points in the path towards the target.</param>
        /// <param name="numCorners">See buffer.</param>
        /// <param name="lastCorner">True if the buffer contains the end point of the path.</param>
        /// <param name="requiresRepath">True if nodes along the path have been destroyed and a path recalculation is necessary.</param>
        public Vector3 Update(Vector3 position, List<Vector3> buffer, int numCorners, out bool lastCorner, out bool requiresRepath)
        {
            lastCorner = default(bool);
            requiresRepath = default(bool);
            return default;
        }

        /// <summary>Cached object to avoid unnecessary allocations</summary>
        static Queue<TriangleMeshNode> navmeshClampQueue = new Queue<TriangleMeshNode>();
        /// <summary>Cached object to avoid unnecessary allocations</summary>
        static List<TriangleMeshNode> navmeshClampList = new List<TriangleMeshNode>();
        /// <summary>Cached object to avoid unnecessary allocations</summary>
        static Dictionary<TriangleMeshNode, TriangleMeshNode> navmeshClampDict = new Dictionary<TriangleMeshNode, TriangleMeshNode>();

        /// <summary>
        /// Searches for the node the agent is inside.
        /// This will also clamp the position to the navmesh
        /// and repair the funnel cooridor if the agent moves slightly outside it.
        ///
        /// Returns: True if nodes along the path have been destroyed so that a path recalculation is required
        /// </summary>
        bool ClampToNavmeshInternal(ref Vector3 position)
        {
            return default;
        }

        /// <summary>
        /// Fill wallBuffer with all navmesh wall segments close to the current position.
        /// A wall segment is a node edge which is not shared by any other neighbour node, i.e an outer edge on the navmesh.
        /// </summary>
        public void FindWalls(List<Vector3> wallBuffer, float range)
        {
        }

        void FindWalls(int nodeIndex, List<Vector3> wallBuffer, Vector3 position, float range)
        {
        }

        bool FindNextCorners(Vector3 origin, int startIndex, List<Vector3> funnelPath, int numCorners, out bool lastCorner)
        {
            lastCorner = default(bool);
            return default;
        }
    }

    public struct FakeTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public class RichSpecial : RichPathPart
    {
        public OffMeshLinks.OffMeshLinkTracer nodeLink;
        public FakeTransform first => new FakeTransform { position = nodeLink.relativeStart, rotation = nodeLink.isReverse ? nodeLink.link.end.rotation : nodeLink.link.start.rotation };
        public FakeTransform second => new FakeTransform { position = nodeLink.relativeEnd, rotation = nodeLink.isReverse ? nodeLink.link.start.rotation : nodeLink.link.end.rotation };
        public bool reverse => nodeLink.isReverse;

        public override void OnEnterPool()
        {
        }

        /// <summary>Works like a constructor, but can be used even for pooled objects. Returns this for easy chaining</summary>
        public RichSpecial Initialize(OffMeshLinks.OffMeshLinkTracer nodeLink)
        {
            return default;
        }
    }
}
