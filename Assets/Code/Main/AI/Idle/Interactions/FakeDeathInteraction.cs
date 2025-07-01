using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class FakeDeathInteraction : INpcInteraction {
        public event Action OnInternalEnd;

        ShareableARAssetReference _animations;
        ARInteractionAnimations _arInteractionAnimations;
        IEventListener _endInteractionListener;

        NpcElement _interactingNpc;
        bool _movingToAbyss;
        bool _isStopping;
        
        bool _changeIntoGhost;
        bool _ifChangedIntoGhostStayInGhost;
        bool _revertGhost;
        
        Vector3 _lastPosition;
        float _startDuration, _endDuration;

        public bool CanBeInterrupted => false;
        public bool AllowBarks => false;
        public bool AllowDialogueAction => false;
        public bool AllowTalk => false;
        public float? MinAngleToTalk => null;
        public int Priority => 99;
        public bool FullyEntered => true;

        public FakeDeathInteraction(Vector3 lastPosition, float startDuration, float endDuration, ARAssetReference animations, bool changeIntoGhost, bool ifChangedIntoGhostStayInGhost) {
            _lastPosition = lastPosition;
            _startDuration = startDuration;
            _endDuration = endDuration;
            _animations = animations.AsShareable();
            _changeIntoGhost = changeIntoGhost;
            _ifChangedIntoGhostStayInGhost = ifChangedIntoGhostStayInGhost;
        }
        
        public Vector3? GetInteractionPosition(NpcElement npc) => npc is { IsUnique: true, NpcPresence: null } ? null : _lastPosition;
        public Vector3 GetInteractionForward(NpcElement npc) => npc.Forward();

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => npc.ParentModel.HasElement<TemporaryDeathElement>();

        public InteractionBookingResult Book(NpcElement npc) {
            if (_interactingNpc == null) {
                _interactingNpc = npc;
                return InteractionBookingResult.ProperlyBooked;
            } else if (_interactingNpc == npc) {
                return InteractionBookingResult.AlreadyBookedBySameNpc;
            } else {
                return InteractionBookingResult.AlreadyBookedByOtherNpc;
            }
        }

        public void Unbook(NpcElement npc) {
            _interactingNpc = null;
        }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            bool instant = false;
            var temporaryDeathElement = npc.ParentModel.Element<TemporaryDeathElement>();
            _endInteractionListener = temporaryDeathElement.ListenTo(TemporaryDeathElement.Events.TemporaryDeathStateChanged, OnUpdatedTemporaryDeath, npc);
            if (temporaryDeathElement.RestoredWhileDead || NpcPresence.InAbyss(npc.Coords)) {
                instant = true;
            }

            if (!npc.HasElement<BlockEnterCombatMarker>()) {
                npc.AddElement<BlockEnterCombatMarker>();
            }

            if (_arInteractionAnimations != null) {
                Log.Critical?.Error($"NPC {npc} is starting interaction which is still waiting for animations to end for {_arInteractionAnimations.Npc}");
                _arInteractionAnimations.UnloadOverride();
            }
            _arInteractionAnimations = new ARInteractionAnimations(npc, _animations);
            _arInteractionAnimations.LoadOverride();
            
            if (instant) {
                if (_changeIntoGhost && npc.TryGetElement<NpcGhostElement>(out var npcGhostElement) && npcGhostElement.Revertable) {
                    _revertGhost = true;
                }
                npc.Movement.Controller.MoveToAbyss();
            } else {
                if (_changeIntoGhost && !npc.HasElement<NpcGhostElement>()) {
                    npc.AddElement(new NpcGhostElement(_startDuration / 2f, true));
                    if (!_ifChangedIntoGhostStayInGhost) {
                        _revertGhost = true;
                    }
                }
                Starting(npc).Forget();
            }
        }

        public void ResumeInteraction(NpcElement npc, InteractionStartReason reason) { }

        async UniTaskVoid Starting(NpcElement npc) {
            if (!await AsyncUtil.WaitWhile(npc, () => _arInteractionAnimations is { IsLoadingOverrides: true })) {
                return;
            }

            if (_arInteractionAnimations == null || _interactingNpc == null) {
                return;
            }
            
            npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomEnter);
            
            if (!await AsyncUtil.DelayTime(npc, _startDuration)) {
                return;
            }
            npc.Movement.Controller.MoveToAbyss();
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            World.EventSystem.TryDisposeListener(ref _endInteractionListener);
            if (reason is InteractionStopReason.MySceneUnloading or InteractionStopReason.NPCPresenceDisabled) {
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
                
                // fake death was interrupted. Treat it as completed.
                if (npc is {HasBeenDiscarded: false, Behaviours: {HasBeenDiscarded: false}}) {
                    var interactionOverride = npc.Behaviours.Elements<InteractionOverride>();
                    var owner = interactionOverride.FirstOrDefault(io => io.Finder is InteractionFakeDeathFinder fakeDeathFinder && fakeDeathFinder.Interaction(npc) == this);
                    owner?.Discard();
                }
                return;
            }
            if (reason == InteractionStopReason.Death) {
                npc.Movement?.Controller.AbortMoveToAbyss();
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
                return;
            }
            if (npc is null or { HasBeenDiscarded: true } or { IsUnique: true, NpcPresence: null } )  {
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
                return;
            }

            _isStopping = true;
            Stopping(npc).Forget();
        }

        public void PauseInteraction(NpcElement npc, InteractionStopReason reason) { }

        async UniTaskVoid Stopping(NpcElement npc) {
            npc.Movement?.Controller.AbortMoveToAbyss();
            npc.ParentModel.SafelyMoveTo(_lastPosition, true);
            npc.ParentModel.SetInteractability(LocationInteractability.Active);
            
            npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomLoop);
            if (!await AsyncUtil.DelayTime(npc, 0.5f)) {
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
                return;
            }
            npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomExit);
            
            if (_revertGhost) {
                npc.TryGetElement<NpcGhostElement>()?.RevertChanges(_endDuration);
            }
            
            if (!await AsyncUtil.DelayTime(npc, _endDuration)) {
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
                return;
            }

            if (_revertGhost) {
                npc.TryGetElement<NpcGhostElement>()?.FinishRevertChanges();
            }

            npc.TryGetElement<BlockEnterCombatMarker>()?.Discard();
            
            _arInteractionAnimations?.UnloadOverride();
            _arInteractionAnimations = null;
            
            _isStopping = false;
        }

        void OnUpdatedTemporaryDeath(bool dead) {
            if (dead) {
                return;
            }

            if (_interactingNpc is { HasBeenDiscarded: false, IsAlive: true } && !_interactingNpc.Interactor.IsInteracting) {
                // Interaction should be paused right now because AI is in Abyss.
                _interactingNpc.Interactor.Perform(this, InteractionStartReason.ResumeInteraction);
            }

            TriggerOnEnd();
            World.EventSystem.TryDisposeListener(ref _endInteractionListener);
        }
        
        void TriggerOnEnd() {
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }
        
        public bool IsStopping(NpcElement npc) => _isStopping;
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}