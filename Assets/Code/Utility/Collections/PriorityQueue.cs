using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Assertions;
using UniversalProfiling;

namespace Awaken.Utility.Collections {
    /// <summary>
    /// Fast removal, peek and pop. Linear add. Linear RePrioritize though effective should be decent. If values don't change extremely.
    /// </summary>
    public class PriorityQueue<TID, TItem> where TID : IComparable {
        static readonly UniversalProfilerMarker RePrioritizeMarker = new("PriorityQueue: Reprioritize");
        
        readonly LinkedList<PQNode> _linkedList = new();
        readonly Dictionary<TID, LinkedListNode<PQNode>> _nodeRefs = new();

        public int Count => _linkedList.Count;
        
        // === Structure Methods
        public void Add(TID id, float priority, TItem item) => Add(new PQNode(id, priority, item));
        public void Add(PQNode node) => Add(new LinkedListNode<PQNode>(node));

        public void Add(LinkedListNode<PQNode> newNode) {
            _nodeRefs.Add(newNode.Value.id, newNode);
            AddSorted(newNode);
        }

        public void AddRange(IEnumerable<PQNode> nodes) {
            var newNodes = nodes.Select(x => new LinkedListNode<PQNode>(x));
            AddRangeSortedRight(_linkedList.First, newNodes);
        }

        public void Remove(TID id) {
            var node = _nodeRefs[id];
            _nodeRefs.Remove(id);
            _linkedList.Remove(node);
        }

        public void RemoveNode(PQNode node) {
            _nodeRefs.Remove(node.id);
            _linkedList.Remove(node);
        }

        public TItem Pop() => PopNode().item;

        public PQNode PopNode() {
            var result = _linkedList.First.Value;
            _nodeRefs.Remove(result.id);
            _linkedList.RemoveFirst();
            return result;
        }

        public void Clear() {
            _linkedList.Clear();
            _nodeRefs.Clear();
        }
        
        public TItem Peek() => _linkedList.First.Value.item;
        public PQNode Read(TID id) => _nodeRefs[id].Value;
        
        /// <summary>
        /// Mostly for optimization combo of Read+RePrioritize. Should not be used unless you want to reuse the dict read
        /// </summary>
        public LinkedListNode<PQNode> ReadInternalNode(TID id) => _nodeRefs[id];

        public void RePrioritize(LinkedListNode<PQNode> node, float priority) {
            RePrioritizeMarker.Begin();
            
            var refNode = node.Next ?? node.Previous;
            node.Value.priority = priority;

            if (refNode == null) {
                RePrioritizeMarker.End();
                return;
            }
            _linkedList.Remove(node);

            bool right = refNode.Value.priority < node.Value.priority;

            if (right) AddSortedRight(refNode, node);
            else AddSortedLeft(refNode, node);
            
            RePrioritizeMarker.End();
        }

        /// <summary>
        /// Removes the node from the internal linked list. Should only be used before running re-prioritize on the nodes.
        /// </summary>
        public void UnlinkNode(PQNode node) => _linkedList.Remove(_nodeRefs[node.id]);

        /// <summary>
        /// Requires the nodes to be unlinked before call
        /// </summary>
        public void RePrioritizeRange(List<PQNode> nodes) {
            RePrioritizeMarker.Begin();
            DEBUG_CheckRePrioritizeRequirements(nodes);

            AddRangeSortedRight(_linkedList.First, nodes.Select(n => _nodeRefs[n.id]));
            RePrioritizeMarker.End();
        }

        public void RePrioritize(TID id, float priority) => RePrioritize(_nodeRefs[id], priority);

        // === Sorting

        void AddSortedLeft(LinkedListNode<PQNode> movingNode, LinkedListNode<PQNode> nodeToAdd) {
            while (movingNode != null) {
                if (nodeToAdd.Value.priority > movingNode.Value.priority) { // Checking if the node we want to add has a higher priority than the left node
                    _linkedList.AddAfter(movingNode, nodeToAdd);
                    return;
                }

                movingNode = movingNode.Previous;
            }

            _linkedList.AddFirst(nodeToAdd);
        }
        
        void AddSortedRight(LinkedListNode<PQNode> movingNode, LinkedListNode<PQNode> nodeToAdd) {
            while (movingNode != null) {
                if (nodeToAdd.Value.priority < movingNode.Value.priority) { // Checking if the node we want to add has a lower priority than the right node
                    _linkedList.AddBefore(movingNode, nodeToAdd);
                    return;
                }

                movingNode = movingNode.Next;
            }

            _linkedList.AddLast(nodeToAdd);
        }

        void AddRangeSortedRight(LinkedListNode<PQNode> movingNode, IEnumerable<LinkedListNode<PQNode>> nodesToAdd) {
            nodesToAdd = nodesToAdd.OrderBy(n => n.Value.priority);
            using var enumerator = nodesToAdd.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            if (movingNode == null) {
                // Handle empty queue case
                movingNode = enumerator.Current;
                _nodeRefs.TryAdd(movingNode.Value.id, movingNode);
                _linkedList.AddFirst(movingNode);
                if (!enumerator.MoveNext()) return;
            }

            while (movingNode is not null) {
                var cur = enumerator.Current;
                // Checking if the node we want to add has a lower priority than the node to the right
                if (cur.Value.priority < movingNode.Value.priority) {
                    _nodeRefs.TryAdd(cur.Value.id, cur);
                    _linkedList.AddBefore(movingNode, cur);
                    if (!enumerator.MoveNext()) return; // No more items to add. End
                } else {
                    movingNode = movingNode.Next;
                }
            }

            // Reached end of linked list. append all other nodes
            do {
                var cur = enumerator.Current;
                _nodeRefs.TryAdd(cur.Value.id, cur);
                _linkedList.AddLast(cur);
            } while (enumerator.MoveNext());
        }

        void AddSorted(LinkedListNode<PQNode> nodeToAdd) => AddSortedRight(_linkedList.First, nodeToAdd);
        
        
        // === Enumerations
        public Enumerator AllNodesBelow(float priority) => new(this, priority);
        public Enumerator GetEnumerator() => new(this);

        
        // === Public Helpers
        public ref struct Enumerator {
            LinkedListNode<PQNode> _index;
            readonly PriorityQueue<TID, TItem> _priorityQueue;
            float _priorityLimit;

            public Enumerator(PriorityQueue<TID, TItem> priorityQueue) {
                _index = null;
                _priorityQueue = priorityQueue;
                _priorityLimit = int.MaxValue;
            }
            public Enumerator(PriorityQueue<TID, TItem> priorityQueue, float allBelowPriority) {
                _index = null;
                _priorityQueue = priorityQueue;
                _priorityLimit = allBelowPriority;
            }

            public Enumerator GetEnumerator() => this;

            public bool MoveNext() => Next() && _index.Value.priority < _priorityLimit;

            public PQNode Current => _index.Value;

            bool Next() {
                _index = _index == null ? _priorityQueue._linkedList.First : _index.Next;
                return _index != null;
            }
        }

        public class PQNode : QueueNode<TID, TItem> {
            public float priority;

            public PQNode(TID id, float priority, TItem item) : base(id, item) {
                this.priority = priority;
            }
        }
        
        //=== Debug
        [Conditional("DEBUG")]
        void DEBUG_CheckRePrioritizeRequirements(List<PQNode> nodes) {
            foreach (var node in nodes) {
                if (_linkedList.Contains(node)) {
                    throw new ArgumentException("RePrioritizeRange requires that nodes are Unlinked before call");
                }
            }
        }
    }
    
    public class QueueNode<TID, TItem> {
        public readonly TID id;
        public readonly TItem item;
        public QueueNode(TID id, TItem item) {
            this.id = id;
            this.item = item;
        }
    }
}