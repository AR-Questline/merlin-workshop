using System;
using System.Runtime.CompilerServices;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Collections {
    /// <summary>
    /// Quadtree with data stored in UnsafeLists, so if it is passed to the functions where
    /// the quadtree is modified, it should be passed by ref.
    /// Contains one element per node. Node is subdivided if in that node falls more than 1 element.
    /// </summary>
    public unsafe struct UnsafeInsertOnlyQuadtree<T> : IDisposable where T : unmanaged {
        UnsafeList<Node> _nodes;
        UnsafeList<T> _datas;
        Rect _rootNodeRect;
        readonly float _dataRadius;
        readonly float _minDistanceSqBetweenPoints;

        public Rect Rect {
            get => _rootNodeRect;
            set => _rootNodeRect = value;
        }

        public T* DatasPtr => _datas.Ptr;
        public int DatasCount => _datas.Length;
        public bool IsCreated => _nodes.IsCreated;

        public void CopyDatasToArray(T[] array) {
            int count = DatasCount;
            if (array.Length < count) {
                Log.Important?.Error($"Provided array size is {array.Length} but it should be at least of length {count}");
                return;
            }

            fixed (T* arrayPtr = array) {
                UnsafeUtility.MemCpy(arrayPtr, _datas.Ptr, sizeof(T) * count);
            }
        }

        public UnsafeInsertOnlyQuadtree(Rect bounds, float dataRadius, float minDistanceBetweenPoints,
            int initialNodesCapacity = 128, int initialDatasCapacity = 64, Allocator allocator = Allocator.Persistent) {
            this._rootNodeRect = bounds;
            _dataRadius = dataRadius;
            _minDistanceSqBetweenPoints = math.square(minDistanceBetweenPoints);
            _nodes = new UnsafeList<Node>(initialNodesCapacity, allocator);
            _nodes.Add(Node.EmptyLeaf());
            _datas = new UnsafeList<T>(initialDatasCapacity, allocator);
        }

        public readonly bool IsOverlappingAny(float2 circleCenter, float radius) {
            var checkedCircleRect = GetCircleRect(in circleCenter, in radius);
            return IsOverlappingAny(0, _rootNodeRect, in checkedCircleRect, in circleCenter, in radius);
        }

        public void Insert(T data, float2 dataCircleCenter) {
            TryInsert(0, _rootNodeRect, data, in dataCircleCenter);
        }

        readonly bool IsOverlappingAny(in int nodeIndex, in Rect nodeRect, in Rect checkedCircleRect, in float2 circleCenter, in float radius) {
            var expandedNodeRect = GetExpandedRect(in nodeRect, in _dataRadius);
            if (expandedNodeRect.Overlaps(checkedCircleRect) == false) {
                return false;
            }

            var node = _nodes[nodeIndex];
            bool isLeaf = node.HasSubNodes == false;
            if (isLeaf) {
                return node.HasData && IsCirclesOverlap(node.DataCircleCenter, in _dataRadius, in circleCenter, in radius);
            }

            GetSubNodesRectsData(nodeRect, out float x, out float y, out float subNodeWidth, out float subNodeHeight);
            var subNode0Rect = new Rect(x, y, subNodeWidth, subNodeHeight);
            if (IsOverlappingAny(node.SubNode0Index, in subNode0Rect, in checkedCircleRect, in circleCenter, in radius)) {
                return true;
            }

            var subNode1Rect = new Rect(x + subNodeWidth, y, subNodeWidth, subNodeHeight);
            if (IsOverlappingAny(node.SubNode1Index, in subNode1Rect, in checkedCircleRect, in circleCenter, in radius)) {
                return true;
            }

            var subNode2Rect = new Rect(x, y + subNodeHeight, subNodeWidth, subNodeHeight);
            if (IsOverlappingAny(node.SubNode2Index, in subNode2Rect, in checkedCircleRect, in circleCenter, in radius)) {
                return true;
            }

            var subNode3Rect = new Rect(x + subNodeWidth, y + subNodeHeight, subNodeWidth, subNodeHeight);
            if (IsOverlappingAny(node.SubNode3Index, in subNode3Rect, in checkedCircleRect, in circleCenter, in radius)) {
                return true;
            }

            return false;
        }

        bool TryInsert(int nodeIndex, Rect nodeRect, in T data, in float2 dataCircleCenter) {
            if (nodeRect.Contains(dataCircleCenter) == false) {
                return false;
            }

            var node = _nodes[nodeIndex];
            bool isLeafNode = node.HasSubNodes == false;
            if (isLeafNode) {
                if (node.HasData == false) {
                    var dataIndex = _datas.Length;
                    _datas.Add(data);
                    InsertDataIntoLeafNode(nodeIndex, dataIndex, dataCircleCenter);
                    return true;
                }

                var nodePrevDataCircleCenter = node.DataCircleCenter;
                if (math.distancesq(nodePrevDataCircleCenter, dataCircleCenter) < _minDistanceSqBetweenPoints) {
                    Log.Important?.Error($"Trying to add circle with center {dataCircleCenter} which is too close (distance = {math.distance(nodePrevDataCircleCenter, dataCircleCenter)}) to previously added circle with center {nodePrevDataCircleCenter}. Minimal allowed distance between centers is {math.sqrt(_minDistanceSqBetweenPoints)}");
                    return false;
                }

                var nodePrevDataIndex = node.DataIndex;
                // When subdividing, data index is cleared, because first subNode index is written to the same
                // field as dataIndex (dataIndex is written with minus sign) 
                Subdivide(ref node);
                _nodes[nodeIndex] = node;
                InsertIntoSubNode(node, nodeRect, nodePrevDataIndex, nodePrevDataCircleCenter);
            }

            GetSubNodesRectsData(nodeRect, out float x, out float y, out float subNodeWidth, out float subNodeHeight);
            var subNode0Rect = new Rect(x, y, subNodeWidth, subNodeHeight);
            if (TryInsert(node.SubNode0Index, subNode0Rect, in data, in dataCircleCenter)) {
                return true;
            }

            var subNode1Rect = new Rect(x + subNodeWidth, y, subNodeWidth, subNodeHeight);
            if (TryInsert(node.SubNode1Index, subNode1Rect, in data, in dataCircleCenter)) {
                return true;
            }

            var subNode2Rect = new Rect(x, y + subNodeHeight, subNodeWidth, subNodeHeight);
            if (TryInsert(node.SubNode2Index, subNode2Rect, in data, in dataCircleCenter)) {
                return true;
            }

            var subNode3Rect = new Rect(x + subNodeWidth, y + subNodeHeight, subNodeWidth, subNodeHeight);
            if (TryInsert(node.SubNode3Index, subNode3Rect, in data, in dataCircleCenter)) {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void InsertIntoSubNode(Node node, Rect nodeRect, int dataIndex, float2 dataCircleCenter) {
            GetSubNodesRectsData(nodeRect, out float x, out float y, out float subNodeWidth, out float subNodeHeight);
            var subNode0Rect = new Rect(x, y, subNodeWidth, subNodeHeight);
            if (subNode0Rect.Contains(dataCircleCenter)) {
                InsertDataIntoLeafNode(node.SubNode0Index, dataIndex, dataCircleCenter);
                return;
            }

            var subNode1Rect = new Rect(x + subNodeWidth, y, subNodeWidth, subNodeHeight);
            if (subNode1Rect.Contains(dataCircleCenter)) {
                InsertDataIntoLeafNode(node.SubNode1Index, dataIndex, dataCircleCenter);
                return;
            }

            var subNode2Rect = new Rect(x, y + subNodeHeight, subNodeWidth, subNodeHeight);
            if (subNode2Rect.Contains(dataCircleCenter)) {
                InsertDataIntoLeafNode(node.SubNode2Index, dataIndex, dataCircleCenter);
                return;
            }

            InsertDataIntoLeafNode(node.SubNode3Index, dataIndex, dataCircleCenter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Rect GetExpandedRect(in Rect nodeRect, in float radius) {
            var expandedNodeRect = nodeRect;
            var radiusExtents = new Vector2(radius, radius);
            expandedNodeRect.min -= radiusExtents;
            expandedNodeRect.max += radiusExtents;
            return expandedNodeRect;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Rect GetCircleRect(in float2 center, in float radius) {
            return new Rect(new Vector2(center.x - radius, center.y - radius), new Vector2(radius * 2, radius * 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsCirclesOverlap(in float2 circleCenter, in float circleRadius, in float2 otherCircleCenter, in float otherCircleRadius) {
            float2 diff = otherCircleCenter - circleCenter;
            float distanceSq = math.dot(diff, diff);
            float radiusSumSq = math.square(circleRadius + otherCircleRadius);
            return distanceSq < radiusSumSq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Subdivide(ref Node node) {
            var subNodesStartIndex = _nodes.Length;
            _nodes.AddReplicate(Node.EmptyLeaf(), 4);
            node.SetSubNodesIndices(subNodesStartIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void InsertDataIntoLeafNode(int nodeIndex, int dataIndex, float2 dataCircleCenter) {
            _nodes[nodeIndex] = Node.Leaf(dataIndex, (half2)dataCircleCenter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetSubNodesRectsData(Rect nodeRect, out float x, out float y, out float subNodeWidth, out float subNodeHeight) {
            x = nodeRect.xMin;
            y = nodeRect.yMin;
            subNodeWidth = nodeRect.width * 0.5f;
            subNodeHeight = nodeRect.height * 0.5f;
        }

        public void Dispose() {
            if (_nodes.IsCreated) {
                _nodes.Dispose();
            }

            if (_datas.IsCreated) {
                _datas.Dispose();
            }
        }

        public struct Node {
            half2 _dataCircleCenterCompressed;
            int _additionalData;
            public int SubNode0Index => _additionalData;
            public int SubNode1Index => _additionalData + 1;
            public int SubNode2Index => _additionalData + 2;
            public int SubNode3Index => _additionalData + 3;
            public int DataIndex => -_additionalData;
            public float2 DataCircleCenter => _dataCircleCenterCompressed;
            public readonly bool HasData => _additionalData != int.MinValue;
            // SubNode index cannot be 0 because root node is a node on index 0
            public readonly bool HasSubNodes => _additionalData > 0 && _additionalData != int.MaxValue;

            public static Node EmptyLeaf() {
                Node node;
                node._dataCircleCenterCompressed = default;
                node._additionalData = int.MinValue;
                return node;
            }

            public static Node Leaf(int dataIndex, half2 dataCircleCenterCompressed) {
                Node node;
                node._dataCircleCenterCompressed = dataCircleCenterCompressed;
                node._additionalData = -dataIndex;
                return node;
            }

            public void SetSubNodesIndices(int subNode0Index) {
                _additionalData = subNode0Index;
            }
        }
    }
}