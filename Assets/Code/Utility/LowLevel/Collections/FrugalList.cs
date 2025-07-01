using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.LowLevel.Collections {
    public struct FrugalList<T> : IFrugalList {
        public ushort TypeForSerialization => SavedTypes.FrugalList;

        [Saved] object _backingElement;

        public int Count => _backingElement switch {
            T => 1,
            UnsafePinnableList<T> multipleItems => multipleItems.Count,
            _ => 0
        };

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public T this[int index] {
            get {
                return _backingElement switch {
                    T singleItem => singleItem,
                    UnsafePinnableList<T> multipleItems => multipleItems[index],
                    _ => throw new System.IndexOutOfRangeException(),
                };
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public T this[uint index] {
            get {
                return _backingElement switch {
                    T singleItem => singleItem,
                    UnsafePinnableList<T> multipleItems => multipleItems[(int)index],
                    _ => throw new System.IndexOutOfRangeException(),
                };
            }
        }

        public void Add(T item) {
            if (_backingElement == null) {
                _backingElement = item;
            } else {
                if (_backingElement is not UnsafePinnableList<T> list) {
                    list = new UnsafePinnableList<T>(4);
                    list.Add((T)_backingElement);
                    _backingElement = list;
                }
                list.Add(item);
            }
        }

        public bool Remove(T item) {
            if (_backingElement == null) {
                return false;
            } else if (_backingElement is T singleElement) {
                if (singleElement.Equals(item)) {
                    _backingElement = null;
                    return true;
                } else {
                    return false;
                }
            } else if(_backingElement is UnsafePinnableList<T> list) {
                if (list.Remove(item)) {
                    if (list.Count == 1) {
                        _backingElement = list[0];
                    }
                    return true;
                }
            }

            return false;
        }

        public bool Contains(T item) {
            return _backingElement switch {
                T singleElement => singleElement.Equals(item),
                UnsafePinnableList<T> list => list.Contains(item),
                _ => false
            };
        }

        public void Clear() {
            if (_backingElement is UnsafePinnableList<T> list) {
                list.Clear();
            }
            _backingElement = null;
        }

        public T FirstOrDefault() {
            return _backingElement switch {
                T singleItem => singleItem,
                UnsafePinnableList<T> multipleItems => multipleItems.FirstOrDefault(),
                _ => default,
            };
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        object IFrugalList.BackingElement {
            get => _backingElement;
            set => _backingElement = value;
        }

        public struct Enumerator {
            private object _backingElement;
            private int _index;

            public Enumerator(FrugalList<T> frugalList) {
                _backingElement = frugalList._backingElement;
                _index = -1;
            }

            public T Current => _backingElement switch {
                T singleItem => singleItem,
                UnsafePinnableList<T> multipleItems => multipleItems[_index],
                _ => default,
            };

            public bool MoveNext() {
                ++_index;
                if (_backingElement is UnsafePinnableList<T> list) {
                    return _index < list.Count;
                }

                return _index == 0 & _backingElement != null;
            }
        }

        public T[] ToArray() {
            return _backingElement switch {
                T singleItem => new[] { singleItem },
                UnsafePinnableList<T> multipleItems => multipleItems.ToArray(),
                _ => Array.Empty<T>(),
            };
        }

#if UNITY_EDITOR
        [ShowInInspector, ReadOnly, ShowIf(nameof(IsSingleElement))] T BackingElement => _backingElement switch {
            T cast => cast,
            _ => default
        };

        [ShowInInspector, ReadOnly, ShowIf(nameof(IsArray))] UnsafePinnableList<T> BackingArray => _backingElement switch {
            UnsafePinnableList<T> cast => cast,
            _ => default
        };

        [ShowInInspector, ReadOnly, ShowIf(nameof(IsEmpty)), HideLabel] string Empty => "Is empty";

        bool IsSingleElement => _backingElement is T;
        bool IsArray => _backingElement is UnsafePinnableList<T>;
        bool IsEmpty => _backingElement == null;
#endif
    }

    public interface IFrugalList {
        object BackingElement { get; set; }
    }
}