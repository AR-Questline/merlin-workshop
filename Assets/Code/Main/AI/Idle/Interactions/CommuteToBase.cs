using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public abstract class CommuteToBase : ITempInteraction {

        const float Speed = 5;

        NpcElement _npc;

        protected readonly ActiveStrategy _activeStrategy;
        protected readonly InactiveStrategy _inactiveStrategy;
        bool _active;
        bool _started;

        public bool FastStart { get; private set; }
        public abstract bool StopOnReach { get; }
        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public event Action OnInternalEnd;

        public CommuteToBase() {
            _activeStrategy = new ActiveStrategy(this);
            _inactiveStrategy = new InactiveStrategy(this);
        }
        
        protected void SetupInternal(Vector3 position, float positionRange, float exitRadiusSq) {
            FastStart = false;
            _inactiveStrategy.Setup(position, positionRange, exitRadiusSq);
            _activeStrategy.Setup(position, positionRange, exitRadiusSq);
        }

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        Vector3 INpcInteraction.GetInteractionForward(NpcElement npc) => Vector3.zero;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);
        public void Unbook(NpcElement npc) => _npc = null;

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            _started = true;
            if (_npc == null) {
                EndWithError(npc, $"Npc is null. Should by {npc}").Forget();
                return;
            }
            if (_npc.Movement == null) {
                EndWithError(_npc, $"Npc.Movement of {_npc} is null").Forget();
                return;
            }
            if (_npc.Movement.Controller == null) {
                EndWithError(_npc, $"Npc.Movement.Controller of {_npc} is null").Forget();
                return;
            }
            _active = _npc.NpcAI is { Working: true };
            if (_active) {
                _activeStrategy.Start();
            } else {
                _inactiveStrategy.Start();
            }
            World.Services.Get<UnityUpdateProvider>().RegisterCommuteToInteraction(this);
        }

        async UniTaskVoid EndWithError(NpcElement npc, string error) {
            Log.Important?.Error(error);
            if (await AsyncUtil.DelayFrame(npc, 8) && _started) {
                End();
            }
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            _started = false;
            World.Services.Get<UnityUpdateProvider>().UnregisterCommuteToInteraction(this);
            if (_active) {
                _activeStrategy.Stop(reason);
            } else {
                _inactiveStrategy.Stop(reason);
            }
        }
        
        public bool IsStopping(NpcElement npc) => false;

        public virtual void UnityUpdate() {
            if (_npc == null || _npc.HasBeenDiscarded) {
                World.Services.Get<UnityUpdateProvider>().UnregisterCommuteToInteraction(this);
                return;
            }
            
            var active = _npc.NpcAI is { Working: true };
            
            if (_active != active) {
                if (active) {
                    _inactiveStrategy.Stop(InteractionStopReason.ChangeInteraction);
                    _activeStrategy.Start();
                } else {
                    _activeStrategy.Stop(InteractionStopReason.ChangeInteraction);
                    _inactiveStrategy.Start();
                }
                _active = active;
            }

            if (_active) {
                _activeStrategy.Update();
            } else {
                _inactiveStrategy.Update();
            }
        }
        
        void End() {
            if (_npc == null) {
                return;
            }
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }

        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;


        protected abstract class Strategy {
            protected readonly CommuteToBase interaction;

            protected Strategy(CommuteToBase interaction) {
                this.interaction = interaction;
            }   
        }
        
        protected class ActiveStrategy : Strategy {
            readonly Wander _wander;

            public ActiveStrategy(CommuteToBase interaction) : base(interaction) {
                _wander = new Wander(CharacterPlace.Default, VelocityScheme.Walk);
            }

            public void Setup(Vector3 position, float positionRange, float exitRadiusSq) {
                if (interaction.StopOnReach) {
                    _wander.OnEnd += interaction.End;
                }
                _wander.UpdateDestination(new CharacterPlace(position, positionRange));
                _wander.UpdateInstantExitRadiusSq(exitRadiusSq);
            }

            public void UpdatePosition(Vector3 position) {
                _wander.UpdateDestination(position, _wander.Destination.Radius);
            }

            public void Start() {
                if (interaction.StopOnReach && Vector3.SqrMagnitude(_wander.Destination.Position - interaction._npc.Coords) < _wander.Destination.Radius) {
                    interaction.FastStart = true;
                    interaction.End();
                    return;
                }
                var movement = interaction._npc.Movement;
                movement.Controller.ToggleIdleOnlyRichAIActivity(true);
                movement.ChangeMainState(_wander);
            }

            public void Stop(InteractionStopReason reason) {
                if (reason == InteractionStopReason.Death) {
                    return;
                }
                interaction._npc?.Movement?.ResetMainState(_wander);
            }

            public void Update() {
                
            }
        }

        protected class InactiveStrategy : Strategy {
            Vector3 _position;
            float _positionRange;
            float _exitRadiusSq;
            bool _constructingPath;
            
            List<Vector3> _path;
            int _index;
            
            public Vector3 Position => _position;

            public InactiveStrategy(CommuteToBase interaction) : base(interaction) { }

            public void Setup(Vector3 position, float positionRange, float exitRadiusSq) {
                _position = position;
                _positionRange = positionRange;
                _exitRadiusSq = exitRadiusSq;
            }
            
            public void UpdatePosition(Vector3 position) {
                _position = position;
                TryConstructPath();
            }
            
            public void Start() {
                TryConstructPath();
            }

            public void Stop(InteractionStopReason reason) {
                _path = null;
                if (reason == InteractionStopReason.Death) {
                    return;
                }
                TryCancelConstructPath();
            }

            public void Update() {
                if (_path == null) {
                    return;
                }
                
                if (_index >= _path.Count) {
                    return; //NPC can't finish Commute To in disabled state
                }
            
                var location = interaction._npc.ParentModel;
                var goal = Vector3.MoveTowards(location.Coords, _path[_index], Speed * Time.deltaTime);
                location.SafelyMoveTo(goal);
            
                if (Vector3.SqrMagnitude(goal - _position) < _exitRadiusSq) {
                    _index = _path.Count + 1;
                } else if (goal == _path[_index]) {
                    _index++;
                }
            }

            void TryConstructPath() {
                if (_constructingPath) {
                    return;
                }
                
                if (ShouldConstructPath()) {
                    _constructingPath = true;
                    interaction._npc.Movement.Controller.GetComponent<Seeker>().StartPath(ABPath.Construct(interaction._npc.Coords, _position), OnAfterRepath);
                } else {
                    _path = null;
                    _constructingPath = false;
                }
            }

            void TryCancelConstructPath() {
                if (!_constructingPath) {
                    return;
                }
                
                _constructingPath = false;
                interaction._npc?.Movement?.Controller.GetComponent<Seeker>().CancelCurrentPathRequest();
            }
            
            bool ShouldConstructPath() {
                // We shouldn't bother in constructing a path if the NPC will reach the destination immediately after entering active state.
                return interaction.StopOnReach && Vector3.SqrMagnitude(_position - interaction._npc.Coords) < _positionRange;
            }
            
            void OnAfterRepath(Path path) {
                if (_constructingPath) {
                    // Path creation was internally cancelled;
                    return;
                }
                _constructingPath = false;
                
                if (path.error && interaction?._npc is { HasBeenDiscarded: false, Interactor: { CurrentInteraction: { } currentInteraction} } npc && currentInteraction == interaction) {
                    Log.Important?.Error($"InactiveCommuteTo: Failed to construct path for {LogUtils.GetDebugName(npc)} from {npc.Coords} to {_position}: {path.errorLog}");
                    npc.ParentModel.SafelyMoveTo(_position);
                    _path = null;
                    return;
                }
                
                _path = path.vectorPath;
                _index = 1;
            }
        }
    }
}