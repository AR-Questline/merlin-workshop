using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Collections {
	/// <summary>
	/// Holds a lookup datastructure to quickly find objects inside rectangles.
	/// Objects of type T occupy an integer rectangle in the grid and they can be
	/// moved efficiently. You can query for all objects that touch a specified
	/// rectangle that runs in O(m*k+r) time where m is the number of objects that
	/// the query returns, k is the average number of cells that an object
	/// occupies and r is the area of the rectangle query.
	///
	/// All objects must be contained within a rectangle with one point at the origin
	/// (inclusive) and one at <see cref="size"/> (exclusive) that is specified in the constructor.
	/// </summary>
	public class GridLookup<T> where T : class {
		Vector2Int size;
		Item[] cells;
		/// <summary>
		/// Linked list of all items.
		/// Note that the first item in the list is a dummy item and does not contain any data.
		/// </summary>
		Root all = new Root();
		Dictionary<T, Root> rootLookup = new Dictionary<T, Root>();
		Stack<Item> itemPool = new Stack<Item>();

		public GridLookup (Vector2Int size) {
        }

        internal class Item {
			public Root root;
			public Item prev, next;
		}

		public class Root {
			/// <summary>Underlying object</summary>
			public T obj;
			/// <summary>Next item in the linked list of all roots</summary>
			public Root next;
			/// <summary>Previous item in the linked list of all roots</summary>
			internal Root prev;
			internal IntRect previousBounds = new IntRect(0, 0, -1, -1);
			/// <summary>References to an item in each grid cell that this object is contained inside</summary>
			internal List<Item> items = new List<Item>();
			internal bool flag;

			public UnityEngine.Vector3 previousPosition = new UnityEngine.Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			public UnityEngine.Quaternion previousRotation;
		}

		/// <summary>Linked list of all items</summary>
		public Root AllItems {
			get {
				return all.next;
			}
		}

		public void Clear () {
        }

        public Root GetRoot (T item) {
            return default;
        }

        /// <summary>
        /// Add an object to the lookup data structure.
        /// Returns: A handle which can be used for Move operations
        /// </summary>
        public Root Add (T item, IntRect bounds) {
            return default;
        }

        /// <summary>Removes an item from the lookup data structure</summary>
        public void Remove(T item)
        {
        }

        public void Dirty(T item)
        {
        }

        /// <summary>Move an object to occupy a new set of cells</summary>
        public void Move(T item, IntRect bounds)
        {
        }

        /// <summary>
        /// Returns all objects of a specific type inside the cells marked by the rectangle.
        /// Note: For better memory usage, consider pooling the list using Pathfinding.Pooling.ListPool after you are done with it
        /// </summary>
        public List<U> QueryRect<U>(IntRect r) where U : class, T
        {
            return default;
        }

        public void Resize(IntRect newBounds)
        {
        }
    }
}
