using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Pooling;

	/// <summary>
	/// Manager for blocker scripts such as SingleNodeBlocker.
	///
	/// This is part of the turn based utilities. It can be used for
	/// any game, but it is primarily intended for turn based games.
	///
	/// See: TurnBasedAI
	/// See: turnbased (view in online documentation for working links)
	/// See: traversal_provider (view in online documentation for working links)
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/blockmanager.html")]
	public class BlockManager : VersionedMonoBehaviour {
		/// <summary>Contains info on which SingleNodeBlocker objects have blocked a particular node</summary>
		Dictionary<GraphNode, List<SingleNodeBlocker> > blocked = new Dictionary<GraphNode, List<SingleNodeBlocker> >();

		public enum BlockMode {
			/// <summary>All blockers except those in the TraversalProvider.selector list will block</summary>
			AllExceptSelector,
			/// <summary>Only elements in the TraversalProvider.selector list will block</summary>
			OnlySelector
		}

		/// <summary>Blocks nodes according to a BlockManager</summary>
		public class TraversalProvider : ITraversalProvider {
			/// <summary>Holds information about which nodes are occupied</summary>
			readonly BlockManager blockManager;

			/// <summary>Affects which nodes are considered blocked</summary>
			public BlockMode mode { get; private set; }

			/// <summary>
			/// Blockers for this path.
			/// The effect depends on <see cref="mode"/>.
			///
			/// Note that having a large selector has a performance cost.
			///
			/// See: mode
			/// </summary>
			readonly List<SingleNodeBlocker> selector;

			public TraversalProvider (BlockManager blockManager, BlockMode mode, List<SingleNodeBlocker> selector) {
            }

            #region ITraversalProvider implementation

            public bool CanTraverse(Path path, GraphNode node)
            {
                return default;
            }

            public bool CanTraverse(Path path, GraphNode from, GraphNode to)
            {
                return default;
            }

            public uint GetTraversalCost(Path path, GraphNode node)
            {
                return default;
            }

            // This can be omitted in Unity 2021.3 and newer because a default implementation (returning true) can be used there.
            public bool filterDiagonalGridConnections {
				get {
					return true;
				}
			}

			#endregion
		}

		void Start () {
        }

        /// <summary>True if the node contains any blocker which is included in the selector list</summary>
        public bool NodeContainsAnyOf (GraphNode node, List<SingleNodeBlocker> selector) {
            return default;
        }

        /// <summary>True if the node contains any blocker which is not included in the selector list</summary>
        public bool NodeContainsAnyExcept(GraphNode node, List<SingleNodeBlocker> selector)
        {
            return default;
        }

        /// <summary>
        /// Register blocker as being present at the specified node.
        /// Calling this method multiple times will add multiple instances of the blocker to the node.
        ///
        /// Note: The node will not be blocked immediately. Instead the pathfinding
        /// threads will be paused and then the update will be applied. It is however
        /// guaranteed to be applied before the next path request is started.
        /// </summary>
        public void InternalBlock(GraphNode node, SingleNodeBlocker blocker)
        {
        }

        /// <summary>
        /// Remove blocker from the specified node.
        /// Will only remove a single instance, calling this method multiple
        /// times will remove multiple instances of the blocker from the node.
        ///
        /// Note: The node will not be unblocked immediately. Instead the pathfinding
        /// threads will be paused and then the update will be applied. It is however
        /// guaranteed to be applied before the next path request is started.
        /// </summary>
        public void InternalUnblock(GraphNode node, SingleNodeBlocker blocker)
        {
        }
    }
}
