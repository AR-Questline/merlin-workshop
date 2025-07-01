using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Debugging;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class Flee : MovementState {
        const int TheGScoreToStopAt = 25000;
        const float RefreshPathEverySeconds = 2.5f;
        
        readonly IGrounded _toAvoid;
        readonly IRotationScheme _rotateTowards;
        readonly RotateTowardsMovement _toMovement = new();
        
        float? _nextPathUpdateTimer;
        bool _wasOverBand4;
        
        public override VelocityScheme VelocityScheme => VelocityScheme.Run;
        
        // === Creation
        public Flee(IGrounded toAvoid, bool rotateTowardsTarget = false) {
            _toAvoid = toAvoid;
            _rotateTowards = rotateTowardsTarget ? new RotateTowardsCombatTarget() : _toMovement;
            if (_toAvoid == null) {
                Log.Important?.Warning("Flee state created with no target to avoid: " + LogUtils.GetDebugName(Npc), Controller);
            }
        }
        
        protected override void OnEnter() {
            if (_toAvoid == null || _toAvoid.HasBeenDiscarded) {
                End();
                return;
            }

            Controller.SetRotationScheme(Npc.Movement.Controller.ForwardMovementOnly ? _toMovement : _rotateTowards, VelocityScheme);
            CalculateFleePath();
        }

        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            if (_nextPathUpdateTimer.HasValue) {
                _nextPathUpdateTimer -= deltaTime;
                if (_nextPathUpdateTimer.Value <= 0) {
                    CalculateFleePath();
                }
            }
        }

        void CalculateFleePath() {
            _nextPathUpdateTimer = null;
            Path path;

            if (Npc.NpcAI.IsOverBand4()) {
                _wasOverBand4 = true;
                path = GetPathToSpawnPoint();
            } else if (_wasOverBand4) {
                if (Npc.NpcAI.IsInBand1()) {
                    _wasOverBand4 = false;
                    path = GetFleePath();
                } else {
                    path = GetPathToSpawnPoint();
                }
            } else {
                path = GetFleePath();
            }

            Controller.Seeker.StartPath(path, OnPathCalculated);
        }

        Path GetPathToSpawnPoint() {
            return ABPath.Construct(Npc.Coords, Npc.LastOutOfCombatPosition);
        }
        
        Path GetFleePath() {
            Vector3 escapeFrom = _toAvoid.Coords;
            FleePath fleePath = FleePath.Construct(Npc.Coords, escapeFrom, TheGScoreToStopAt);
            fleePath.aimStrength = 1;
            fleePath.spread = 4000;
            return fleePath;
        }

        void OnPathCalculated(Path path) {
            if (path.error) {
                Log.Important?.Error($"Failed to create flee path for: {Npc}", Npc.Controller);
                return;
            }
            
            Controller.RichAI.SetPath(path);
            _nextPathUpdateTimer = RefreshPathEverySeconds;
        }
    }
}