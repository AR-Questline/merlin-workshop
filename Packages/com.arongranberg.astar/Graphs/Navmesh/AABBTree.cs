// #define VALIDATE_AABB_TREE
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Pathfinding.Collections {
	/// <summary>
	/// Axis Aligned Bounding Box Tree.
	///
	/// Holds a bounding box tree with arbitrary data.
	///
	/// The tree self-balances itself regularly when nodes are added.
	/// </summary>
	public class AABBTree<T> {
		Node[] nodes = new Node[0];
		int root = NoNode;
		readonly Stack<int> freeNodes = new Stack<int>();
		int rebuildCounter = 64;
		const int NoNode = -1;

		struct Node {
			public Bounds bounds;
			public uint flags;
			const uint TagInsideBit = 1u << 30;
			const uint TagPartiallyInsideBit = 1u << 31;
			const uint AllocatedBit = 1u << 29;
			const uint ParentMask = ~(TagInsideBit | TagPartiallyInsideBit | AllocatedBit);
			public const int InvalidParent = (int)ParentMask;
			public bool wholeSubtreeTagged {
				get => (flags & TagInsideBit) != 0;
				set => flags = (flags & ~TagInsideBit) | (value ? TagInsideBit : 0);
			}
			public bool subtreePartiallyTagged {
				get => (flags & TagPartiallyInsideBit) != 0;
				set => flags = (flags & ~TagPartiallyInsideBit) | (value ? TagPartiallyInsideBit : 0);
			}
			public bool isAllocated {
				get => (flags & AllocatedBit) != 0;
				set => flags = (flags & ~AllocatedBit) | (value ? AllocatedBit : 0);
			}
			public bool isLeaf => left == NoNode;
			public int parent {
				get => (int)(flags & ParentMask);
				set => flags = (flags & ~ParentMask) | (uint)value;
			}
			public int left;
			public int right;
			public T value;
		}

		/// <summary>A key to a leaf node in the tree</summary>
		public readonly struct Key {
			internal readonly int value;
			public int node => value - 1;
			public bool isValid => value != 0;
			internal Key(int node) : this()
            {
            }
        }

        static float ExpansionRequired(Bounds b, Bounds b2)
        {
            return default;
        }

        /// <summary>User data for a node in the tree</summary>
        public T this[Key key] => nodes[key.node].value;

		/// <summary>Bounding box of a given node</summary>
		public Bounds GetBounds (Key key)
        {
            return default;
        }

        int AllocNode()
        {
            return default;
        }

        void FreeNode(int node)
        {
        }

        /// <summary>
        /// Rebuilds the whole tree.
        ///
        /// This can make it more balanced, and thus faster to query.
        /// </summary>
        public void Rebuild()
        {
        }

        /// <summary>Removes all nodes from the tree</summary>
        public void Clear()
        {
        }

        struct AABBComparer : IComparer<int>
        {
            public Node[] nodes;
            public int dim;

            public int Compare(int a, int b) => nodes[a].bounds.center[dim].CompareTo(nodes[b].bounds.center[dim]);
		}

        static int ArgMax (Vector3 v) {
            return default;
        }

        int Rebuild(UnsafeSpan<int> leaves, int parent)
        {
            return default;
        }

        /// <summary>
        /// Moves a node to a new position.
        ///
        /// This will update the tree structure to account for the new bounding box.
        /// This is equivalent to removing the node and adding it again with the new bounds, but it preserves the key value.
        /// </summary>
        /// <param name="key">Key to the node to move</param>
        /// <param name="bounds">New bounds of the node</param>
        public void Move(Key key, Bounds bounds)
        {
        }

        [System.Diagnostics.Conditional("VALIDATE_AABB_TREE")]
        void Validate(int node)
        {
        }

        public Bounds Remove(Key key)
        {
            return default;
        }

        public Key Add(Bounds bounds, T value)
        {
            return default;
        }

        /// <summary>Queries the tree for all objects that touch the specified bounds.</summary>
        /// <param name="bounds">Bounding box to search within</param>
        /// <param name="buffer">The results will be added to the buffer</param>
        public void Query(Bounds bounds, List<T> buffer) => QueryNode(root, bounds, buffer);

		void QueryNode (int node, Bounds bounds, List<T> buffer) {
        }

        /// <summary>Queries the tree for all objects that have been previously tagged using the <see cref="Tag"/> method.</summary>
        /// <param name="buffer">The results will be added to the buffer</param>
        /// <param name="clearTags">If true, all tags will be cleared after this call. If false, the tags will remain and can be queried again later.</param>
        public void QueryTagged(List<T> buffer, bool clearTags = false) => QueryTaggedNode(root, clearTags, buffer);

		void QueryTaggedNode (int node, bool clearTags, List<T> buffer) {
        }

        /// <summary>
        /// Tags a particular object.
        ///
        /// Any previously tagged objects stay tagged.
        /// You can retrieve the tagged objects using the <see cref="QueryTagged"/> method.
        /// </summary>
        /// <param name="key">Key to the object to tag</param>
        public void Tag (Key key) {
        }

        /// <summary>
        /// Tags all objects that touch the specified bounds.
        ///
        /// Any previously tagged objects stay tagged.
        /// You can retrieve the tagged objects using the <see cref="QueryTagged"/> method.
        /// </summary>
        /// <param name="bounds">Bounding box to search within</param>
        public void Tag(Bounds bounds) => TagNode(root, bounds);

		bool TagNode (int node, Bounds bounds) {
            return default;
        }
    }
}
