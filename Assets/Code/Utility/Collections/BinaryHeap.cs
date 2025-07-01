using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Extensions;
using Unity.Profiling;
using UniversalProfiling;

namespace Awaken.Utility.Collections {
    /// <summary>
    /// Collection representing array based min-heap.
    /// Its head which you get with Extract or Peek is always the smallest element in heap.
    /// It is not serializable due to IComparer it needs to order its elements.
    /// </summary>
    public class BinaryHeap<T> : IEnumerable<T> {
        List<T> _elements;
        IComparer<T> _comparer;

        public BinaryHeap(IComparer<T> comparer, int initSize = -1) {
            _comparer = comparer;
            _elements = initSize != -1 
                            ? new List<T>(initSize) 
                            : new List<T>();
            Clear();
        }

        /// <summary>
        /// Count of elements in heap
        /// </summary>
        public int Size => _elements.Count - 1;
        
        /// <summary>
        /// Is there no element in heap
        /// </summary>
        public bool IsEmpty => Size == 0;
        
        /// <summary>
        /// Returns smallest element in heap without removing it
        /// </summary>
        public T Peek => IsEmpty ? default : _elements[1];

        // == Operations
        
        /// <summary>
        /// Adds new element to heap
        /// </summary>
        public void Insert(T element) {
            _elements.Add(element);
            HeapifyUp(Size);
        }
        
        /// <summary>
        /// Removes element from heap
        /// </summary>
        public void Remove(T element) {
            int removeIndex = _elements.IndexOf(element);
            if (removeIndex >= 1) {
                MakeRoot(removeIndex);
                Extract();
            }
        }
        
        /// <summary>
        /// Removes smallest element from heap and returns it
        /// </summary>
        public T Extract() {
            if (IsEmpty) throw new Exception("Extracting data from empty heap");

            T head = _elements[1];
            _elements[1] = _elements[Size];
            _elements.RemoveAt(Size);
            Heapify.Begin();
            HeapifyDown(1);
            Heapify.End();
            return head;
        }
        
        static readonly UniversalProfilerMarker Heapify = new ("Heapify");
        
        /// <summary>
        /// Clears heap
        /// </summary>
        public void Clear() {
            _elements.Clear();
            _elements.Add(default);
        }

        // == Heapifying
        
        void HeapifyUp(int index) {
            while (index > 1) {
                int parent = index / 2;

                if (_comparer.Compare(_elements[parent], _elements[index]) > 0) {
                    _elements.Swap(parent, index);
                } else {
                    return;
                }

                index = parent;
            }
        }

        void HeapifyDown(int index) {
            int left = index * 2;
            int right = left + 1;
            while (left < Size) {
                int smallest = _comparer.Compare(_elements[left], _elements[right]) < 0 ? left : right;
                
                if (_comparer.Compare(_elements[index], _elements[smallest]) > 0) {
                    _elements.Swap(index, smallest);
                } else {
                    return;
                }

                index = smallest;
                left = index * 2;
                right = left + 1;
            }
            if (left == Size && _comparer.Compare(_elements[index], _elements[left]) > 0) {
                _elements.Swap(index, left);
            }
        }
        
        void MakeRoot(int index) {
            while (index > 1) {
                int parent = index / 2;
                _elements.Swap(parent, index);
                index = parent;
            }
        }

        // == IEnumerable 
        
        public IEnumerator<T> GetEnumerator() {
            return _elements.Skip(1).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}