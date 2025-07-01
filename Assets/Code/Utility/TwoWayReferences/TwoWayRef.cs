using System.Collections;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Utility.TwoWayReferences {
    public abstract class TwoWayRef<TLeft, TRight> 
        where TLeft : class, TwoWayRef<TLeft, TRight>.ILeft
        where TRight : class, TwoWayRef<TLeft, TRight>.IRight
    {
        static readonly EnumerableCache<TLeft> ReusableLefts = new(16);
        static readonly EnumerableCache<TRight> ReusableRights = new(16);
        
        public interface ILeft {
            ref LeftStorage Storage { get; }
        }
        
        public interface IRight {
            ref RightStorage Storage { get; }
        }

        public struct LeftStorage : IEnumerable<TRight> {
            // ReSharper disable once InconsistentNaming
            public static LeftStorage Null = new();

            readonly TLeft _owner;
            internal UnsafePinnableList<Pair> refs;

            public LeftStorage(TLeft owner) {
                _owner = owner;
                refs = new();
            }

            public readonly Enumerator GetEnumerator() => new(refs);
            public readonly EnumerableCache<TRight>.Enumerator GetCached() => ReusableRights.Cache(this);

            public bool Add(TRight other) {
                if (Contains(other)) {
                    return false;
                }
                var newRef = new Pair { left = _owner, right = other };
                refs.Add(newRef);
                other.Storage.refs.Add(newRef);
                return true;
            }

            public void Remove(TRight other) {
                if (TryFindIndex(other, out var index)) {
                    refs[index].right.Storage.RemoveFromLeftRemoval(_owner);
                    refs.SwapBackRemove(index);
                }
            }
            
            public void Clear() {
                for (int i = refs.Count - 1; i >= 0; i--) {
                    refs[i].right.Storage.RemoveFromLeftRemoval(_owner);
                }
                refs.Clear();
            }

            public readonly bool Contains(TRight other) => TryFindIndex(other, out _);
            public readonly int Count() => refs.Count;
            public readonly bool Any() => refs.Count > 0;
            public readonly bool IsEmpty() => refs.Count == 0;
            
            internal void RemoveFromRightRemoval(TRight other) {
                if (TryFindIndex(other, out int index)) {
                    refs.SwapBackRemove(index);
                }
            }

            readonly bool TryFindIndex(TRight other, out int index) {
                for (int i = 0; i < refs.Count; i++) {
                    if (refs[i].right == other) {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
            
            IEnumerator<TRight> IEnumerable<TRight>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<TRight> {
                readonly UnsafePinnableList<Pair> _refs;
                int _index;
                
                internal Enumerator(UnsafePinnableList<Pair> refs) {
                    _refs = refs;
                    _index = -1;
                }

                public TRight Current => _refs[_index].right;
                public bool MoveNext() => ++_index < _refs.Count;
                public void Dispose() { }
                public void Reset() { }
                
                object IEnumerator.Current => Current;
            }
        }
        
        public struct RightStorage : IEnumerable<TLeft> {
            // ReSharper disable once InconsistentNaming
            public static RightStorage Null = new();

            readonly TRight _owner;
            internal UnsafePinnableList<Pair> refs;
            
            public RightStorage(TRight owner) {
                _owner = owner;
                refs = new();
            }

            public readonly Enumerator GetEnumerator() => new(refs);
            public readonly EnumerableCache<TLeft>.Enumerator GetCached() => ReusableLefts.Cache(this);

            public bool Add(TLeft other) {
                if (Contains(other)) {
                    return false;
                }
                var newRef = new Pair { left = other, right = _owner };
                refs.Add(newRef);
                other.Storage.refs.Add(newRef);
                return true;
            }

            public void Remove(TLeft other) {
                if (TryFindIndex(other, out var index)) {
                    refs[index].left.Storage.RemoveFromRightRemoval(_owner);
                    refs.SwapBackRemove(index);
                }
            }

            public void Clear() {
                for (int i = refs.Count - 1; i >= 0; i--) {
                    refs[i].left.Storage.RemoveFromRightRemoval(_owner);
                }
                refs.Clear();
            }

            public readonly bool Contains(TLeft other) => TryFindIndex(other, out _);
            public readonly int Count() => refs.Count;
            public readonly bool Any() => refs.Count > 0;
            public readonly bool IsEmpty() => refs.Count == 0;
            
            internal void RemoveFromLeftRemoval(TLeft other) {
                if (TryFindIndex(other, out var index)) {
                    refs.SwapBackRemove(index);
                }
            }

            readonly bool TryFindIndex(TLeft other, out int index) {
                for (int i = 0; i < refs.Count; i++) {
                    if (refs[i].left == other) {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }

            IEnumerator<TLeft> IEnumerable<TLeft>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            
            public struct Enumerator : IEnumerator<TLeft> {
                readonly UnsafePinnableList<Pair> _refs;
                int _index;
                
                internal Enumerator(UnsafePinnableList<Pair> refs) {
                    _refs = refs;
                    _index = -1;
                }

                public TLeft Current => _refs[_index].left;
                public bool MoveNext() => ++_index < _refs.Count;
                public void Dispose() { }
                public void Reset() { }
                
                object IEnumerator.Current => Current;
            }
        }
        
        internal struct Pair {
            public TLeft left;
            public TRight right;
        }
    }
}