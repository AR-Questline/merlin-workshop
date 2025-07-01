using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions.Patrols {
    [DisallowMultipleComponent]
    public class GroupPatrol : MonoBehaviour, IPatrolPathContainer {
        [SerializeField, HideInInspector] Formation formation;
        [SerializeField] PatrolPath path = PatrolPath.Default;
        [SerializeField] float accelerationOverride = 4f;

        const float NormalDeviation = 0.5f;
        const float TrotDeviation = 1.5f;
        const float RunDeviation = 5f;

        const float FarAway2FarBehind = -RunDeviation * RunDeviation;
        const float FarBehind2Behind = -TrotDeviation * TrotDeviation;
        const float Behind2Correct = -NormalDeviation * NormalDeviation;
        const float Correct2Ahead = NormalDeviation * NormalDeviation;
        const float Ahead2FarAhead = TrotDeviation * TrotDeviation;
        const float FarAhead2FarAway = RunDeviation* RunDeviation;

        const float ExtendDeviationFactor = 0.1f;

        public GroupPatrolSpot[] Spots { get; private set; }

        List<GroupPatrolSpot> _activeSpots;
        List<SpotState> _spotStates;

        GroupPatrolSpot _leadingSpot;
        Action _onWaypointReached;

        PatrolPath.Index _reachedIndex;
        PatrolPath.Index _nextWaypoint;

        bool _endingPatrol;
        bool _isMoving;

        public ref PatrolPath PatrolPath => ref path;
        public float AccelerationOverride => accelerationOverride;

        void Start() {
            path.Init(transform);
            Spots = GetComponents<GroupPatrolSpot>();
            _activeSpots = new List<GroupPatrolSpot>(Spots.Length);
            _spotStates = new List<SpotState>(Spots.Length);
            _onWaypointReached = OnWaypointReached;
        }

        public void AddActiveSpot(GroupPatrolSpot spot) {
            if (_activeSpots.Count == 0) {
                StartPatrol(spot.Npc);
            }

            _activeSpots.Add(spot);
            _spotStates.Add(default);

            if (_leadingSpot is null || spot.Priority > _leadingSpot.Priority) {
                ChangeLeadingSpot(spot);
            }
        }

        public void RemoveActiveSpot(GroupPatrolSpot spot) {
            if (_endingPatrol) {
                return;
            }

            int index = _activeSpots.IndexOf(spot);
            if (index < 0) {
                Log.Important?.Error("Removing spot that is not active", this);
                return;
            }

            var removedSpot = _activeSpots[index];
            _activeSpots.RemoveAt(index);
            _spotStates.RemoveAt(index);

            if (_activeSpots.Count == 0) {
                ChangeLeadingSpot(null);
                EndPatrol();
            } else if (removedSpot == _leadingSpot) {
                ChangeLeadingSpot(_activeSpots.MaxBy(static spot => spot.Priority, true));
            }
        }

        void ChangeLeadingSpot(GroupPatrolSpot spot) {
            if (_leadingSpot is not null) {
                _leadingSpot.OnWaypointReached -= _onWaypointReached;
            }

            _leadingSpot = spot;
            if (_leadingSpot is not null) {
                _leadingSpot.OnWaypointReached += _onWaypointReached;
            }
        }

        void StartPatrol(NpcElement npc) {
            path.RetrieveClosestPathToPoint(npc.Coords, out _, out _, out _reachedIndex);
            MoveToNextWaypoint();
        }

        void EndPatrol() {
            _isMoving = false;
        }

        void MoveToNextWaypoint() {
            if (path.TryGetNextIndex(_reachedIndex, out _nextWaypoint)) {
                _isMoving = true;
            } else {
                _endingPatrol = true;
                foreach (var spot in _activeSpots) {
                    spot.EndPatrol();
                }

                _endingPatrol = false;
                _activeSpots.Clear();
                _spotStates.Clear();
                ChangeLeadingSpot(null);
                EndPatrol();
            }
        }

        void OnWaypointReached() {
            _isMoving = false;
            _reachedIndex = _nextWaypoint;
            // TODO: add waypoint interactions here
            MoveToNextWaypoint();
        }

        void Update() {
            if (!_isMoving) {
                return;
            }

            var rotation = Quaternion.FromToRotation(Vector3.forward, path.GetInterpolatedForward(_reachedIndex, _leadingSpot.Npc.Coords));
            var groupCenter = _leadingSpot.Npc.Coords - rotation * _leadingSpot.Offset.ToHorizontal3();

            var leadingForward = _leadingSpot.Npc.Forward();

            bool anybodyBehind = false;
            bool anybodyAhead = false;
            bool anybodyFarBehind = false;
            bool anybodyFarAhead = false;
            bool anybodyFarAway = false;

            for (int i = 0; i < _activeSpots.Count; i++) {
                var spot = _activeSpots[i];
                var state = _spotStates[i];
                var position = spot.Npc.Coords;
                var desiredPosition = spot.OffsetPoint(groupCenter, rotation);
                var distanceSq = position.SquaredDistanceTo(desiredPosition) / spot.DeviationSq;
                bool ahead = Vector3.Dot(position - desiredPosition, leadingForward) > 0;
                var signedDistanceSq = ahead ? distanceSq : -distanceSq;

                state.relativePosition = state.relativePosition switch {
                    RelativePosition.FarFarAway => GetRelativePositionFromFarAway(signedDistanceSq),
                    RelativePosition.FarBehind => GetRelativePositionFromFarBehind(signedDistanceSq),
                    RelativePosition.Behind => GetRelativePositionFromBehind(signedDistanceSq),
                    RelativePosition.Correct => GetRelativePositionFromCorrect(signedDistanceSq),
                    RelativePosition.Ahead => GetRelativePositionFromAhead(signedDistanceSq),
                    RelativePosition.FarAhead => GetRelativePositionFromFarAhead(signedDistanceSq),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (state.relativePosition == RelativePosition.FarFarAway) {
                    anybodyFarAway = true;
                } else if (state.relativePosition == RelativePosition.FarBehind) {
                    anybodyFarBehind = true;
                } else if (state.relativePosition == RelativePosition.Behind) {
                    anybodyBehind = true;
                } else if (state.relativePosition == RelativePosition.Ahead) {
                    anybodyAhead = true;
                } else if (state.relativePosition == RelativePosition.FarAhead) {
                    anybodyFarAhead = true;
                }

                _spotStates[i] = state;
            }

            if (anybodyFarAway) {
                for (int i = 0; i < _activeSpots.Count; i++) {
                    if (_spotStates[i].relativePosition is RelativePosition.Correct) {
                        _activeSpots[i].Wait();
                    } else  {
                        _activeSpots[i].MoveToGroup(groupCenter, rotation, VelocityScheme.Trot);
                    }
                }
            } else {
                ref readonly var waypoint = ref path.GetWaypoint(_nextWaypoint);
                if (anybodyFarBehind) {
                    for (int i = 0; i < _activeSpots.Count; i++) {
                        var velocity = _spotStates[i].relativePosition switch {
                            RelativePosition.FarBehind => VelocityScheme.Run,
                            RelativePosition.Behind => VelocityScheme.Trot,
                            RelativePosition.Correct => VelocityScheme.SlowWalk,
                            RelativePosition.Ahead => VelocityScheme.NoMove,
                            RelativePosition.FarAhead => VelocityScheme.NoMove,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        _activeSpots[i].MoveToNextWaypoint(waypoint.position, waypoint.rotation, velocity);
                    }
                } else if (anybodyFarAhead) {
                    for (int i = 0; i < _activeSpots.Count; i++) {
                        var velocity = _spotStates[i].relativePosition switch {
                            RelativePosition.Behind => VelocityScheme.Run,
                            RelativePosition.Correct => VelocityScheme.Trot,
                            RelativePosition.Ahead => VelocityScheme.Walk,
                            RelativePosition.FarAhead => VelocityScheme.NoMove,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        _activeSpots[i].MoveToNextWaypoint(waypoint.position, waypoint.rotation, velocity);
                    }
                } else if (anybodyBehind) {
                    for (int i = 0; i < _activeSpots.Count; i++) {
                        var velocity = _spotStates[i].relativePosition switch {
                            RelativePosition.Behind => VelocityScheme.Trot,
                            RelativePosition.Correct => VelocityScheme.Walk,
                            RelativePosition.Ahead => VelocityScheme.SlowWalk,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        _activeSpots[i].MoveToNextWaypoint(waypoint.position, waypoint.rotation, velocity);
                    }
                } else if (anybodyAhead) {
                    for (int i = 0; i < _activeSpots.Count; i++) {
                        var velocity = _spotStates[i].relativePosition switch {
                            RelativePosition.Correct => VelocityScheme.Walk,
                            RelativePosition.Ahead => VelocityScheme.SlowWalk,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        _activeSpots[i].MoveToNextWaypoint(waypoint.position, waypoint.rotation, velocity);
                    }
                } else {
                    for (int i = 0; i < _activeSpots.Count; i++) {
                        var velocity = _spotStates[i].relativePosition switch {
                            RelativePosition.Correct => VelocityScheme.Walk,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        _activeSpots[i].MoveToNextWaypoint(waypoint.position, waypoint.rotation, velocity);
                    }
                }

            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            path.EDITOR_DrawGizmos(UnityEditor.Selection.objects.Contains(gameObject));
        }
#endif

        struct SpotState {
            public RelativePosition relativePosition;
        }

        enum RelativePosition : byte {
            FarFarAway,
            FarBehind,
            Behind,
            Correct,
            Ahead,
            FarAhead,
        }

        static RelativePosition GetRelativePositionFromFarAway(float signedDistanceSq) {
            if (signedDistanceSq is > Behind2Correct + ExtendDeviationFactor and < Correct2Ahead - ExtendDeviationFactor) {
                return RelativePosition.Correct;
            }
            return RelativePosition.FarFarAway;
        }
        
        static RelativePosition GetRelativePositionFromFarBehind(float signedDistanceSq) {
            return signedDistanceSq switch {
                < FarAway2FarBehind - ExtendDeviationFactor => RelativePosition.FarFarAway,
                < FarBehind2Behind + ExtendDeviationFactor => RelativePosition.FarBehind,
                _ => RelativePosition.Behind,
            };
        }
        
        static RelativePosition GetRelativePositionFromBehind(float signedDistanceSq) {
            return signedDistanceSq switch {
                < FarBehind2Behind - ExtendDeviationFactor => RelativePosition.FarBehind,
                < Behind2Correct + ExtendDeviationFactor => RelativePosition.Behind,
                _ => RelativePosition.Correct,
            };
        }
        
        static RelativePosition GetRelativePositionFromCorrect(float signedDistanceSq) {
            return signedDistanceSq switch {
                < Behind2Correct - ExtendDeviationFactor => RelativePosition.Behind,
                < Correct2Ahead + ExtendDeviationFactor => RelativePosition.Correct,
                _ => RelativePosition.Ahead,
            };
        }
        
        static RelativePosition GetRelativePositionFromAhead(float signedDistanceSq) {
            return signedDistanceSq switch {
                < Correct2Ahead - ExtendDeviationFactor => RelativePosition.Correct,
                < Ahead2FarAhead + ExtendDeviationFactor => RelativePosition.Ahead,
                _ => RelativePosition.FarAhead,
            };
        }
        
        static RelativePosition GetRelativePositionFromFarAhead(float signedDistanceSq) {
            return signedDistanceSq switch {
                < Ahead2FarAhead - ExtendDeviationFactor => RelativePosition.Ahead,
                < FarAhead2FarAway + ExtendDeviationFactor => RelativePosition.FarAhead,
                _ => RelativePosition.FarFarAway,
            };
        }

        public enum Formation {
            _0,
            _1,
            _2_SingleFile,
            _2_SideBySide,
            _3_SingleFile,
            _3_SideBySide,
            _3_Arrow,
            _3_InversedArrow,
            _4_PeopleSquare,
            _4_PeopleDiamond,
        }
    }
}