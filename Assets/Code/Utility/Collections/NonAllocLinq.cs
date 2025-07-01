using System;
using System.Collections.Generic;

namespace Awaken.Utility.Collections {
	/// <summary>
	/// Linq-like extension class for Collection, which uses smart enumeration to prevent Linq allocation
	/// For HashSet HashSet.Enumerator is struct (so there is no allocation) and allows to iterate over hashset without allocations
	/// Linq methods don't use this special route for hashsets, that mean it's allocating/boxing
	/// For others collections, like List, Linq methods are using special route, so there is no allocations
	/// </summary>
	public static class NonAllocLinq {
		public static T FirstNonAlloc<T>(this HashSet<T> hashSet) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (hashSet.Count < 1) {
				throw new InvalidOperationException("Empty hashset");
			}
			foreach (var element in hashSet) {
				return element;
			}
			// Never reach here but needs for compiler 
			return default;
		}
		
		public static T FirstNonAlloc<T>(this List<T> list) {
			if (list == null) {
				throw new ArgumentNullException(nameof(list));
			}
			if (list.Count < 1) {
				throw new InvalidOperationException("Empty list");
			}
			return list[0];
		}
		
		public static T FirstNonAlloc<T>(this HashSet<T> hashSet, Func<T, bool> predicate) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			if (hashSet.Count < 1) {
				throw new InvalidOperationException("Empty hashset");
			}
			foreach (var element in hashSet) {
				if (predicate(element)) {
					return element;
				}
			}
			throw new InvalidOperationException("No element satisfied the condition");
		}
		
		public static T FirstOrDefaultNonAlloc<T>(this HashSet<T> hashSet, T defaultValue = default) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			foreach (var element in hashSet) {
				return element;
			}
			return defaultValue;
		}
		
		public static T FirstOrDefaultNonAlloc<T>(this HashSet<T> hashSet, Func<T, bool> predicate, T defaultValue = default) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (var element in hashSet) {
				if (predicate(element)) {
					return element;
				}
			}
			return defaultValue;
		}

		public static TPredicate FirstOrDefaultCastNonAlloc<TElem, TPredicate>(
			this HashSet<TElem> hashSet, Func<TPredicate, bool> predicate, TPredicate defaultValue = default) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (TElem element in hashSet) {
				if (element is TPredicate casted && predicate(casted)) {
					return casted;
				}
			}
			return defaultValue;
		}
		
		public static TPredicate FirstOrDefaultCastNonAlloc<TElem, TPredicate>(
			this List<TElem> list, Func<TPredicate, bool> predicate, TPredicate defaultValue = default) {
			if (list == null) {
				throw new ArgumentNullException(nameof(list));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (TElem element in list) {
				if (element is TPredicate casted && predicate(casted)) {
					return casted;
				}
			}
			return defaultValue;
		}
		
		public static bool AnyNonAlloc<T>(this HashSet<T> hashSet) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			return hashSet.Count > 0;
		}
		
		public static bool AnyNonAlloc<T>(this List<T> list) {
			if (list == null) {
				throw new ArgumentNullException(nameof(list));
			}
			return list.Count > 0;
		}
		
		public static bool AnyNonAlloc<T>(this HashSet<T> hashSet, Func<T, bool> predicate) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (var element in hashSet) {
				if (predicate(element)) {
					return true;
				}
			}
			return false;
		}

		public static bool AnyCastNonAlloc<TElem, TPredicate>(this HashSet<TElem> hashSet, Func<TPredicate, bool> predicate) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (var element in hashSet) {
				if (element is TPredicate casted && predicate(casted)) {
					return true;
				}
			}
			return false;
		}

		public static bool AllCastNonAlloc<TElem, TPredicate>(this HashSet<TElem> hashSet, Func<TPredicate, bool> predicate) {
			if (hashSet == null) {
				throw new ArgumentNullException(nameof(hashSet));
			}
			if (predicate == null) {
				throw new ArgumentNullException(nameof(predicate));
			}
			foreach (var element in hashSet) {
				if (element is TPredicate casted && !predicate(casted)) {
					return false;
				}
			}
			return true;
		}
	}
}
