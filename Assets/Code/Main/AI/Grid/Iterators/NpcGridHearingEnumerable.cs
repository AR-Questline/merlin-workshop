using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public readonly struct NpcGridHearingEnumerable : IEnumerable<NpcElement> {
        readonly NpcGrid _grid;
        readonly Vector3 _center;
        readonly float _radius;

        public NpcGridHearingEnumerable(NpcGrid grid, Vector3 center, float radius) {
            _grid = grid;
            _center = center;
            _radius = radius;
        }

        public Enumerator GetEnumerator() => new(_grid, _center, _radius);
        
        IEnumerator<NpcElement> IEnumerable<NpcElement>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<NpcElement> {
            const float MaxAdditionalHearingRange = 15;
            
            readonly NpcGrid _grid;

            readonly Vector3 _center;
            readonly float _radius;
            readonly float _maxRadiusSq;
            
            readonly int _xMin;
            readonly int _xMax;
            readonly int _yMin;
            readonly int _yMax;

            int _xCurrent;
            int _yCurrent;

            float _minDistanceXSq;
            float _minDistanceYSq;

            List<NpcElement> _currentEntries;
            int _currentEntryIndex;

            public NpcElement Current => _currentEntries[_currentEntryIndex];
            object IEnumerator.Current => Current;
            
            public Enumerator(NpcGrid grid, Vector3 center, float radius) {
                var maxRadius = radius + MaxAdditionalHearingRange;
                
                _grid = grid;
                _center = center;
                _radius = radius;
                _maxRadiusSq = math.square(maxRadius);
                
                NpcGridEnumerableUtils.SphereBounds(_grid, center, maxRadius, out _xMin, out _xMax, out _yMin, out _yMax);

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
                    var npc = _currentEntries[_currentEntryIndex];
                    
                    if (npc.HasBeenDiscarded || !npc.HasCompletelyInitialized) {
                        continue;
                    }
                    
                    var ai = npc.NpcAI;
                    if (ai == null) {
                        continue;
                    }
                    var radius = (_radius + ai.Data.perception.MaxHearingRange) * ai.ParentModel.NpcStats.Hearing;
                    var distanceSq = npc.Coords.SquaredDistanceTo(_center);
                    if (distanceSq < math.square(radius)) {
                        return true;
                    }
                }
                return false;
            }

            bool MoveNextChunkY() {
                while (++_yCurrent <= _yMax) {
                    _minDistanceYSq = CalculateMinDistanceYSq();
                    if (_minDistanceXSq + _minDistanceYSq > _maxRadiusSq) {
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
                _currentEntries = _grid.Chunks[_grid.CalculateIndex(new int2(_xCurrent, _yCurrent))].Npcs;
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