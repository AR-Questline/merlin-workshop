using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions.SimpleInteractionAttachments;
﻿using System.Linq;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions.Saving;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions.Patrols {
    [DisallowMultipleComponent]
    public class PatrolInteraction : NpcInteraction, IPatrolPathContainer, ISavableInteraction {
        [SerializeField, AnimancerAnimationsAssetReference] ShareableARAssetReference lookAroundOverrides;
        [SerializeField, ShowIf(nameof(LookAroundOverridesIsSet))] bool increaseLookAroundVision;
        [SerializeField] PatrolPath path = PatrolPath.Default;
        [SerializeField] PatrolSearchType startSearchType = PatrolSearchType.ClosestPath;
        
        Wander _wander;
        PatrolPath.Index _reachedIndex;
        PatrolPath.Index _nextWaypoint;
        
        CustomSimpleInteraction _lookAroundInteraction;
        
        bool LookAroundOverridesIsSet => lookAroundOverrides is { IsSet: true };
        public ref PatrolPath PatrolPath => ref path;
        public override int Priority => 2;
        
        void Start() {
            _wander = new Wander(CharacterPlace.Default, VelocityScheme.Walk);
            _wander.OnEnd += OnWaypointReached;
            path.Init(transform);
        }

        public override Vector3? GetInteractionPosition(NpcElement npc) {
            path.RetrieveClosestPathToPoint(npc.Coords, out var position, out _, out _);
            return position;
        }

        public override Vector3 GetInteractionForward(NpcElement npc) {
            path.RetrieveClosestPathToPoint(npc.Coords, out _, out var forward, out _);
            return forward;
        }

        protected override void OnStart(NpcElement npc, InteractionStartReason reason) {
            if (TryLoadAndSetupSavedData(npc, reason)) {
                MoveToNextWaypoint();
                return;
            }
            
            switch (startSearchType) {
                case PatrolSearchType.ClosestPoint:
                    path.RetrieveClosestWaypoint(npc.Coords, out var index);
                    _reachedIndex = new PatrolPath.Index(index.backWay, index.index - 1);
                    break;
                case PatrolSearchType.ClosestPath:
                    path.RetrieveClosestPathToPoint(npc.Coords, out _, out _, out _reachedIndex);
                    break;
                case PatrolSearchType.AlwaysFirst:
                    _reachedIndex = new PatrolPath.Index(false, -1);
                    break;
            }

            MoveToNextWaypoint();
        }
        
        public bool TryLoadAndSetupSavedData(NpcElement npc, InteractionStartReason startReason) {
            if (startReason is not InteractionStartReason.NPCReactivatedFromGameLoad) {
                return false;
            }
            var behaviours = npc.Behaviours;
            if (behaviours.TryGetSavedInteractionData() is PatrolInteractionSavedData data) {
                _reachedIndex = data.Waypoint;
                return PatrolPath.waypoints.Length > _reachedIndex.index;
            }
            return false;
        }

        protected override void OnEnd(NpcElement npc, InteractionStopReason reason) { }

        public SavedInteractionData SaveData(NpcElement npc) {
            if (npc != _interactingNpc) {
                return null;
            }
            return new PatrolInteractionSavedData() {
                backWay = _reachedIndex.backWay,
                index = _reachedIndex.index
            };
        }
        
        protected override void OnResume(NpcElement npc, InteractionStartReason reason) {
            MoveToNextWaypoint();
        }

        protected override void OnPause(NpcElement npc, InteractionStopReason reason) { }

        public override InteractionBookingResult Book(NpcElement npc) {
            _reachedIndex = default;
            return this.BookOneNpc(ref _interactingNpc, npc);
        }
        
        public override void Unbook(NpcElement npc) {
            _interactingNpc = null;
            ResetInteractingNpcCustomAnimationsFsm();
        }

        void MoveToNextWaypoint() {
            if (path.TryGetNextIndex(_reachedIndex, out _nextWaypoint)) {
                _wander.UpdateDestination(path.GetWaypoint(_nextWaypoint).position);
                _interactingNpc?.Movement?.ChangeMainState(_wander);
            } else {
                End();
            }
        }

        void OnWaypointReached() {
            _reachedIndex = _nextWaypoint;
            ref readonly var waypoint = ref path.GetWaypoint(_reachedIndex);
            if (waypoint.interactAround) {
                InteractAround(waypoint);
                return;
            }
            if (waypoint.lookAroundTime > 0){
                LookAround(waypoint);
                return;
            }
            MoveToNextWaypoint();
        }

        void InteractAround(in PatrolWaypoint waypoint) {
            var interaction = new InteractionBaseFinder(IdlePosition.World(waypoint.position), waypoint.interactionRange, waypoint.interactionTag, null, true, false).FindInteraction(_interactingNpc.Behaviours);
            if (interaction != null) {
                _interactingNpc.Behaviours.PushToStack(interaction);
            } else {
                MoveToNextWaypoint();
            }
        }

        void LookAround(in PatrolWaypoint waypoint) {
            if (lookAroundOverrides is { IsSet: true }) {
                if (_lookAroundInteraction == null) {
                    if (gameObject.GetComponentInChildren<CustomSimpleInteraction>() is {} lookAroundInteraction) {
                        _lookAroundInteraction = lookAroundInteraction;
                    } else {
                        var customInteractionGameObject = new GameObject("Look Around Interaction");
                        customInteractionGameObject.transform.SetParent(transform);
                        _lookAroundInteraction = customInteractionGameObject.AddComponent<CustomSimpleInteraction>();
                    }
                    if (increaseLookAroundVision && !_lookAroundInteraction.gameObject.TryGetComponent<GuardLookoutInteractionAttachment>(out _)) {
                        _lookAroundInteraction.gameObject.AddComponent<GuardLookoutInteractionAttachment>();
                    }
                }
                _lookAroundInteraction.Setup(waypoint.position, waypoint.forward, lookAroundOverrides, waypoint.lookAroundTime);
                _interactingNpc.Behaviours.PushToStack(_lookAroundInteraction);
            } else {
                var stand = new StandInteraction(IdlePosition.World(waypoint.position), IdlePosition.Self, null, waypoint.lookAroundTime);
                _interactingNpc.Behaviours.PushToStack(stand);
            }
        }

        enum PatrolSearchType : byte {
            ClosestPoint,
            ClosestPath,
            AlwaysFirst,
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            path.EDITOR_DrawGizmos(UnityEditor.Selection.objects.Contains(gameObject));
        }

        [Button]
        void AlignGameObjectWithStartOfPath() {
            path.EDITOR_AlignTransformWithStartOfPath(transform);
        }
        
        [Button]
        void AlignStartOfPathWithGameObject() {
            path.waypoints[0].position = transform.position;
        }
        
        [Button]
        void SnapPointsToGround() {
            for (int i = 0; i < path.waypoints.Length; i++) {
                path.waypoints[i].position = Grounds.Ground.SnapNpcToGround(path.waypoints[i].position);
            }
        }
#endif
    }
    
    [Serializable]
    public partial class PatrolInteractionSavedData : SavedInteractionData {
        public override ushort TypeForSerialization => SavedTypes.PatrolInteractionSavedData;

        [Saved] internal bool backWay;
        [Saved] internal int index;

        internal PatrolPath.Index Waypoint => new(backWay, index);
        
        public override INpcInteraction TryToGetInteraction(IdleBehaviours behaviours) {
            if (behaviours.CurrentFinder is InteractionBaseFinder baseFinder) {
                return baseFinder.FindInteractionAfterLoad(behaviours, typeof(PatrolInteraction));
            }
            return null;
        }
    }
}