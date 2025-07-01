using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public readonly struct NpcGridChunkProximityEnumerable : IEnumerable<NpcChunk> {
        readonly NpcGrid _grid;
        readonly int2 _center;
        readonly int _lookupDistance;

        public NpcGridChunkProximityEnumerable(NpcGrid grid, int2 center, int lookupDistance) {
            _grid = grid;
            _center = center;
            _lookupDistance = lookupDistance;
        }

        public Enumerator GetEnumerator() => new(_grid, _center, _lookupDistance);
        
        IEnumerator<NpcChunk> IEnumerable<NpcChunk>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<NpcChunk> {
            static readonly int2[][] OffsetsCache = {
                GenerateOffset(0),
                GenerateOffset(1),
                GenerateOffset(2),
                GenerateOffset(3),
                GenerateOffset(4),
                GenerateOffset(5),
            };

            readonly NpcGrid _grid;
            readonly int2 _center;
            readonly int2[] _offsets;
            int _index;
            
            public NpcChunk Current { get; private set; }

            object IEnumerator.Current => Current;
            
            public Enumerator(NpcGrid grid, int2 center, int lookupDistance) {
                _grid = grid;
                _center = center;
                _offsets = OffsetsCache[lookupDistance];
                _index = -1;
                Current = null;
            }
            
            public bool MoveNext() {
                while (_index < _offsets.Length) {
                    _index++;
                    if (_grid.TryGetChunk(_center + _offsets[_index], out var chunk)) {
                        Current = chunk;
                        return true;
                    }
                }
                return false;
            }

            public void Reset() { }
            public void Dispose() { }

            static int2[] GenerateOffset(int max) {
                int size = max * 2 + 1;
                int2[] offsets = new int2[size * size];
                int index = 0;
                for (int x = -max; x <= max; x++) {
                    for (int y = -max; y <= max; y++) {
                        offsets[index++] = new int2(x, y);
                    }
                }
                Array.Sort(offsets, (a, b) => math.lengthsq(a).CompareTo(math.lengthsq(b)));
                return offsets;
            }
        }
    }
    
}