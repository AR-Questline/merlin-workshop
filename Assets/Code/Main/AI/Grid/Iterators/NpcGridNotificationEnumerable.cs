using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public readonly struct NpcGridNotificationEnumerable : IEnumerable<NpcElement> {
        readonly NpcGrid _grid;
        readonly NpcAI _notifier;
        readonly float _coreRadius;
        readonly float _maxRadius;

        public NpcGridNotificationEnumerable(NpcGrid grid, NpcAI notifier, float coreRadius, float maxRadius) {
            _grid = grid;
            _notifier = notifier;
            _coreRadius = coreRadius;
            _maxRadius = maxRadius;
        }

        public Enumerator GetEnumerator() => new(_grid, _notifier, _coreRadius, _maxRadius);
        
        IEnumerator<NpcElement> IEnumerable<NpcElement>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<NpcElement> {
            const float CoreInAlertNotifyRangeBonus = 15;
            const float MaxInAlertNotifyRangeBonus = 25;
            
            readonly NpcGrid _grid;
            readonly NpcAI _notifier;

            readonly Vector3 _center;
            readonly float _coreRadiusSq;
            readonly float _maxRadiusSq;
            readonly float _coreRadiusSqInAlert;
            readonly float _maxRadiusSqInAlert;
            
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
            
            public Enumerator(NpcGrid grid, NpcAI notifier, float coreRadius, float maxRadius) {
                _grid = grid;
                _notifier = notifier;
                _center = notifier.Coords;
                _coreRadiusSq = coreRadius * coreRadius;
                _maxRadiusSq = maxRadius * maxRadius;
                _coreRadiusSqInAlert = math.square(coreRadius + CoreInAlertNotifyRangeBonus);
                _maxRadiusSqInAlert = math.square(maxRadius + MaxInAlertNotifyRangeBonus);

                NpcGridEnumerableUtils.SphereBounds(grid, _center, maxRadius + MaxInAlertNotifyRangeBonus, out _xMin, out _xMax, out _yMin, out _yMax);
                
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
                    var ai = npc.NpcAI;
                    
                    if (npc.HasBeenDiscarded || !npc.HasCompletelyInitialized) {
                        continue;
                    }
                    
                    if (ai != null && ai.Working && !ai.InCombat && !ai.InFlee) {
                        bool inAlert = ai.AlertStack.AlertValue > 0;
                        var distanceSq = npc.Coords.SquaredDistanceTo(_center);
                        if (inAlert) {
                            if (distanceSq < _coreRadiusSqInAlert || (distanceSq < _maxRadiusSqInAlert && ai.CanSee(_notifier) != VisibleState.Covered)) {
                                return true;
                            }
                        } else {
                            if (distanceSq < _coreRadiusSq || (distanceSq < _maxRadiusSq && ai.CanSee(_notifier) != VisibleState.Covered)) {
                                return true;
                            }
                        }
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