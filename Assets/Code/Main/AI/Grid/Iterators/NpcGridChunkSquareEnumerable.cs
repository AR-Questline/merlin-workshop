using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public readonly struct NpcGridChunkSquareEnumerable : IEnumerable<NpcChunk> {
        readonly NpcGrid _grid;
        
        readonly int _xMin;
        readonly int _xMax;
        readonly int _yMin;
        readonly int _yMax;

        public NpcGridChunkSquareEnumerable(NpcGrid grid, int xMin, int xMax, int yMin, int yMax) {
            _grid = grid;
            _xMin = xMin;
            _xMax = xMax;
            _yMin = yMin;
            _yMax = yMax;
        }
        
        public Enumerator GetEnumerator() => new(_grid, _xMin, _xMax, _yMin, _yMax);
        
        IEnumerator<NpcChunk> IEnumerable<NpcChunk>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<NpcChunk> {
            readonly NpcGrid _grid;
            
            readonly int _xMin;
            readonly int _xMax;
            readonly int _yMin;
            readonly int _yMax;

            int _xCurrent;
            int _yCurrent;
            
            public NpcChunk Current => _grid.GetChunkUnchecked(new int2(_xCurrent, _yCurrent));
            object IEnumerator.Current => Current;

            public Enumerator(NpcGrid grid, int xMin, int xMax, int yMin, int yMax) {
                _grid = grid;
                
                _xMin = math.max(xMin, _grid.Center.x - _grid.GridHalfSize);
                _xMax = math.min(xMax, _grid.Center.x + _grid.GridHalfSize);
                _yMin = math.max(yMin, _grid.Center.y - _grid.GridHalfSize);
                _yMax = math.min(yMax, _grid.Center.y + _grid.GridHalfSize);

                _xCurrent = _xMin;
                _yCurrent = _yMin;
            }

            public bool MoveNext() {
                _yCurrent++;
                if (_yCurrent <= _yMax) {
                    return true;
                }

                _xCurrent++;
                if (_xCurrent <= _xMax) {
                    _yCurrent = _yMin;
                    return true;
                }

                return false;
            }
            
            public void Reset() { }
            
            public void Dispose() { }
        }
    }
}