using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Util;
using UnityEngine.Assertions;

namespace Pathfinding {
	using System;
	using Pathfinding.Drawing;
	using Unity.Jobs;

	/// <summary>
	/// Graph for off-mesh links.
	///
	/// This is an internal graph type which is used to store off-mesh links.
	/// An off-mesh link between two nodes A and B is represented as: <code> A <--> N1 <--> N2 <--> B </code>.
	/// where N1 and N2 are two special nodes added to this graph at the exact start and endpoints of the link.
	///
	/// This graph is not persistent. So it will never be saved to disk and a new one will be created each time the game starts.
	///
	/// It is also not possible to query for the nearest node in this graph. The <see cref="GetNearest"/> method will always return an empty result.
	/// This is by design, as all pathfinding should start on the navmesh, not on an off-mesh link.
	///
	/// See: <see cref="OffMeshLinks"/>
	/// See: <see cref="NodeLink2"/>
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class LinkGraph : NavGraph {
		LinkNode[] nodes = new LinkNode[0];
		int nodeCount;

		public override bool isScanned => true;

		public override bool persistent => false;

		public override bool showInInspector => false;

		public override int CountNodes() => nodeCount;

		protected override void DestroyAllNodes () {
        }

        public override void GetNodes (Action<GraphNode> action)
        {
        }

        internal LinkNode AddNode()
        {
            return default;
        }

        internal void RemoveNode(LinkNode node)
        {
        }

        public override float NearestNodeDistanceSqrLowerBound(Vector3 position, NNConstraint constraint = null) => float.PositiveInfinity;

        /// <summary>
        /// It's not possible to query for the nearest node in a link graph.
        /// This method will always return an empty result.
        /// </summary>
        public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, float maxDistanceSqr) => default;

        public override void OnDrawGizmos(DrawingData gizmos, bool drawNodes, RedrawScope redrawScope)
        {
        }

        class LinkGraphUpdatePromise : IGraphUpdatePromise
        {
            public LinkGraph graph;

			public void Apply (IGraphUpdateContext ctx) {
            }

            public IEnumerator<JobHandle> Prepare() => null;
		}

		protected override IGraphUpdatePromise ScanInternal () => new LinkGraphUpdatePromise { graph = this };
	}

	public class LinkNode : PointNode {
		public OffMeshLinks.OffMeshLinkSource linkSource;
		public OffMeshLinks.OffMeshLinkConcrete linkConcrete;
		public int nodeInGraphIndex;

		public LinkNode() {
        }

        public LinkNode(AstarPath active) : base(active)
        {
        }

        public override void RemovePartialConnection(GraphNode node)
        {
        }

        public override void Open(Path path, uint pathNodeIndex, uint gScore)
        {
        }

        public override void OpenAtPoint(Path path, uint pathNodeIndex, Int3 pos, uint gScore)
        {
        }
    }
}
