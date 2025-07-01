using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions.Patrols {
    [HideMonoScript]
    public class GroupPatrolSpot : NpcInteraction {

        const string PatrolGroup = "Patrol";
        #if UNITY_EDITOR
        [SerializeField, DisplayAsString, HideLabel, PropertyOrder(-2)] new string name;
        public void EDITOR_SetName(string name) => this.name = name;
        #endif
        
        [SerializeField, ReadOnly, FoldoutGroup(PatrolGroup, -1)] int priority;
        [SerializeField, ReadOnly, FoldoutGroup(PatrolGroup)] Vector2 offset;
        [SerializeField, ReadOnly, FoldoutGroup(PatrolGroup)] float deviation = 0.8f;
        
        GroupPatrol _patrol;

        Wander _wanderToWaypoint;
        Wander _wanderToGroup;
        NoMove _noMove;

        float _cachedAcceleration;
        
        public new int Priority => priority;
        public Vector2 Offset => offset;
        public float Deviation => deviation;
        public float DeviationSq => deviation * deviation;
        public NpcElement Npc => _interactingNpc;

        public event Action OnWaypointReached {
            add => _wanderToWaypoint.OnEnd += value;
            remove => _wanderToWaypoint.OnEnd -= value;
        }

        public override Vector3? GetInteractionPosition(NpcElement npc) {
            return transform.TransformPoint(Offset.ToHorizontal3());
        }

        public void SetData(int priority, Vector2 offset) {
            this.priority = priority;
            this.offset = offset;
        }

        void Start() {
            _patrol = GetComponent<GroupPatrol>();
            if (_patrol == null) {
                Log.Important?.Error($"Cannot find GroupPatrol for GroupPatrolSpot {name}", this);
            }
            _wanderToWaypoint = new Wander(CharacterPlace.Default, VelocityScheme.Walk);
            _wanderToGroup = new Wander(CharacterPlace.Default, VelocityScheme.Walk);
            _noMove = new NoMove();
        }

        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return _patrol != null && SimpleAvailableFor(npc, finder) && IsBestSpotFor(npc, finder);
        }
        
        bool SimpleAvailableFor(NpcElement npc, IInteractionFinder finder) {
            return base.AvailableFor(npc, finder);
        }
        
        bool IsBestSpotFor(NpcElement npc, IInteractionFinder finder) {
            foreach (var spot in _patrol.Spots) {
                if (spot != this && spot.SimpleAvailableFor(npc, finder) && spot.priority > priority) {
                    return false;
                }
            }
            return true;
        }

        protected override void OnStart(NpcElement npc, InteractionStartReason reason) {
            var richAI = npc.Movement.Controller.RichAI;
            _cachedAcceleration = richAI.acceleration;
            richAI.acceleration = _patrol.AccelerationOverride;
            _patrol.AddActiveSpot(this);
        }

        protected override void OnEnd(NpcElement npc, InteractionStopReason reason) {
            _patrol.RemoveActiveSpot(this);
            
            if (Npc is { HasBeenDiscarded: false, Movement: not null }) {
                var movement = Npc.Movement;
                movement.ResetMainState(_wanderToWaypoint);
                movement.ResetMainState(_wanderToGroup);
                movement.ResetMainState(_noMove);
                movement.Controller.RichAI.acceleration = _cachedAcceleration;
            }
            _cachedAcceleration = -1;
        }

        public void MoveToNextWaypoint(Vector3 waypoint, Quaternion rotation, VelocityScheme velocityScheme) {
            _wanderToWaypoint.UpdateDestination(OffsetPoint(waypoint, rotation), Deviation * 0.7f);
            _wanderToWaypoint.UpdateVelocityScheme(velocityScheme);
            Npc.Movement.ChangeMainState(_wanderToWaypoint);
        }
        
        public void MoveToGroup(Vector3 point, Quaternion rotation, VelocityScheme velocityScheme) {
            _wanderToGroup.UpdateDestination(OffsetPoint(point, rotation), Deviation * 0.7f);
            _wanderToGroup.UpdateVelocityScheme(velocityScheme);
            Npc.Movement.ChangeMainState(_wanderToGroup);
        }
        
        public void Wait() {
            Npc.Movement.ChangeMainState(_noMove);
        }

        public void EndPatrol() {
            End();
        }

        public Vector3 OffsetPoint(Vector3 point, Quaternion rotation) {
            return point + rotation * Offset.ToHorizontal3();
        }
    }
}