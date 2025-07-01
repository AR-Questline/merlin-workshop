using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public readonly struct NpcGridSphereEnumerable<TEntry, TChunk> : IEnumerable<TEntry> where TEntry : IGrounded where TChunk : INpcChunk<TEntry> {
        readonly NpcGrid _grid;
        readonly TChunk[] _chunks;
        readonly Vector3 _center;
        readonly float _radius;

        public NpcGridSphereEnumerable(NpcGrid grid, TChunk[] chunks, Vector3 center, float radius) {
            _grid = grid;
            _chunks = chunks;
            _center = center;
            _radius = radius;
        }

        public Enumerator GetEnumerator() => new(_grid, _chunks, _center, _radius);
        
        IEnumerator<TEntry> IEnumerable<TEntry>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<TEntry> {
            readonly NpcGrid _grid;
            readonly TChunk[] _chunks;

            readonly Vector3 _center;
            readonly float _radiusSq;
            
            readonly int _xMin;
            readonly int _xMax;
            readonly int _yMin;
            readonly int _yMax;

            int _xCurrent;
            int _yCurrent;

            float _minDistanceXSq;
            float _minDistanceYSq;

            List<TEntry> _currentEntries;
            int _currentEntryIndex;

            public TEntry Current => _currentEntries[_currentEntryIndex];
            object IEnumerator.Current => Current;
            
            public Enumerator(NpcGrid grid, TChunk[] chunks, Vector3 center, float radius) {
                _grid = grid;
                _chunks = chunks;
                _center = center;
                _radiusSq = radius * radius;

                NpcGridEnumerableUtils.SphereBounds(grid, center, radius, out _xMin, out _xMax, out _yMin, out _yMax);

                _xCurrent = _xMin;
                _yCurrent = _yMin;
                _minDistanceXSq = 0;
                _minDistanceYSq = 0;

                _currentEntries = null;
                _currentEntryIndex = -1;

                _minDistanceXSq = CalculateMinDistanceXSq();
                _minDistanceYSq = CalculateMinDistanceYSq();
                RefreshEntryIterator();
            }
            
            public bool MoveNext() {
                return MoveNextNpcInChunk() || MoveNextChunkY() || MoveNextChunkX();
            }

            public void Reset() { }
            
            public void Dispose() { }

            bool MoveNextNpcInChunk() {
                while (++_currentEntryIndex < _currentEntries.Count) {
                    if (_currentEntries[_currentEntryIndex].HasBeenDiscarded) {
                        continue;
                    }

                    if (_currentEntries[_currentEntryIndex] is NpcElement { HasCompletelyInitialized: false }) {
                        continue;
                    }
                    
                    var distanceSq = _currentEntries[_currentEntryIndex].Coords.SquaredDistanceTo(_center);
                    if (distanceSq < _radiusSq) {
                        return true;
                    }
                }
                return false;
            }

            bool MoveNextChunkY() {
                while (++_yCurrent <= _yMax) {
                    _minDistanceYSq = CalculateMinDistanceYSq();
                    if (_minDistanceXSq + _minDistanceYSq > _radiusSq) {
                        continue;
                    }
                    RefreshEntryIterator();
                    if (MoveNextNpcInChunk()) {
                        return true;
                    }
                }
                return false;
            }

            bool MoveNextChunkX() {
                while (++_xCurrent <= _xMax) {
                    _minDistanceXSq = CalculateMinDistanceXSq();
                    _yCurrent = _yMin - 1;
                    if (MoveNextChunkY()) {
                        return true;
                    }
                }
                return false;
            }
            
            void RefreshEntryIterator() {
                _currentEntries = _chunks[_grid.CalculateIndex(new int2(_xCurrent, _yCurrent))].GetEntries();
                _currentEntryIndex = -1;
            }
            
            
            float CalculateMinDistanceXSq() {
                return NpcGridEnumerableUtils.AxisDistanceToSq(_grid, _xCurrent, _center.x);
            }

            float CalculateMinDistanceYSq() {
                return NpcGridEnumerableUtils.AxisDistanceToSq(_grid, _yCurrent, _center.z);
            }
        }
    }
}