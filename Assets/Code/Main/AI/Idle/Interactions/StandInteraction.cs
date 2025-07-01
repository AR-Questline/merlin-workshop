using System;
using System.Threading;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.States.Rotation;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class StandInteraction : INpcInteraction {
        readonly IdlePosition _position;
        readonly IdlePosition _forward;
        readonly IIdleDataSource _source;
        readonly float _duration;

        NpcElement _interactingNpc;
        Wander _wander;
        SnapToPositionAndRotate _snapToPosition;
        CancellationTokenSource _token;
        bool _active;

        public event Action OnInternalEnd;
        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public StandInteraction(IdlePosition position, IdlePosition forward, IIdleDataSource source, float duration) : this(position, forward, source) {
            _duration = duration;
        }
        
        public StandInteraction(IdlePosition position, IdlePosition forward, IIdleDataSource source) {
            _position = position;
            _forward = forward;
            _source = source;
        }

        public Vector3? GetInteractionPosition(NpcElement npc) => GetPosition(npc.Behaviours);
        public Vector3 GetInteractionForward(NpcElement npc) => GetForward(npc.Behaviours);

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) {
            if (_interactingNpc != null) {
                if (_interactingNpc == npc) {
                    return InteractionBookingResult.AlreadyBookedBySameNpc;
                }
                Log.Important?.Error($"Trying to book {this} for {npc} while it is already booked for {_interactingNpc}");
                return InteractionBookingResult.AlreadyBookedByOtherNpc;
            }
            
            _interactingNpc = npc;
            
            return InteractionBookingResult.ProperlyBooked;
        }

        public void Unbook(NpcElement npc) {
            _interactingNpc = null;
        }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            _active = true;
            if (_duration > 0f && _token == null) {
                _token = new CancellationTokenSource();
                DelayEnd(npc, _duration).Forget();
            }
            
            CreateMovementStatesIfNeeded(npc);
            npc.Movement.ChangeMainState(_wander);
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            _active = false;
            _token?.Cancel();
            _token = null;
            if (reason == InteractionStopReason.Death) return;
            npc?.Movement?.ResetMainState(_snapToPosition);
            npc?.Movement?.ResetMainState(_wander);
        }
        
        async UniTaskVoid DelayEnd(NpcElement npc, float delay) {
            if (await AsyncUtil.DelayTime(npc, delay, source: _token)) {
                TriggerOnEnd();
                _token = null;
            }
        }
        
        public bool IsStopping(NpcElement npc) => false;
        
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
        
        public void TriggerOnEnd() {
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }

        public Vector3 GetPosition(IdleBehaviours behaviours) => _position.WorldPosition(behaviours.Location, _source);
        public Vector3 GetForward(IdleBehaviours behaviours) => _forward.WorldForward(behaviours.Location, _source);

        void CreateMovementStatesIfNeeded(NpcElement npc) {
            if (_wander != null) return;
            var behaviours = npc.Behaviours;
            Vector3 position = GetPosition(behaviours);
            Vector3 forward = GetForward(behaviours);
            _wander = new Wander(new CharacterPlace(position, behaviours.PositionRange), VelocityScheme.Walk);
            _snapToPosition = new SnapToPositionAndRotate(position, forward, null);
            _wander.OnEnd += () => OnReachedStandPosition(npc);
        }

        void OnReachedStandPosition(NpcElement npc) {
            if (!_active) {
                return;
            }
            NpcRotate.TryEnterRotationState(npc, GetForward(npc.Behaviours));
            npc.Movement.ChangeMainState(_snapToPosition);
        }
    }
}