using System;
using System.Threading;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Behaviours {
    public partial class NpcInteractor : Element<NpcElement> {
        public const float SnapToInteractionRange = 0.1f;

        public sealed override bool IsNotSaved => true;

        public INpcInteraction CurrentInteraction { get; private set; }
        
        INpcInteraction _stoppingInteraction;
        State _state;
        KeepPosition _keepPosition;
        bool _walkingToPosition;
        InteractionStartReason _interactionStartReason;
        CancellationTokenSource _interactPrepareToken;

        NpcElement Npc => ParentModel;
        NpcMovement Movement => Npc.Movement;

        public bool IsGoingToInteraction => _state == State.GoToInteraction;
        public bool IsInteracting => _state is State.InteractFully or State.InteractPrepare;
        public bool IsFullyInteracting => _state == State.InteractFully;
        public bool NpcInInteractState => Npc.NpcAI is { Working: false } or { InIdle: true, InCombat: false } && Npc is { IsUnconscious: false };
        public bool WalkingToPosition => _walkingToPosition;

        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<NpcElement, InteractionChangedInfo> InteractionChanged = new(nameof(InteractionChanged));
            public static readonly Event<NpcElement, INpcInteraction> CurrentInteractionFullyEntered = new(nameof(CurrentInteractionFullyEntered));
        }
        
        protected override void OnInitialize() {
            _keepPosition = new KeepPosition(CharacterPlace.Default, VelocityScheme.Walk);
            _keepPosition.OnReached += OnPositionReached;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            try {
                Stop(InteractionStopReason.Death, false);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public bool TryToPerformAgain(INpcInteraction interaction, bool forceResume, out bool useRotateFirst) {
            if (_state == State.NotInteracting && (CurrentInteraction == null || CurrentInteraction == interaction)) {
                if (interaction is ITempInteraction || RotateToInteraction.InCorrectRotation(Npc, interaction)) {
                    Perform(interaction, forceResume ? InteractionStartReason.ResumeInteraction : _interactionStartReason);
                    useRotateFirst = false;
                } else {
                    useRotateFirst = true;
                }
                return true;
            }
            useRotateFirst = false;
            return false;
        }

        public void Perform(INpcInteraction interaction, InteractionStartReason startReason) {
            Stop(InteractionStopReason.ChangeInteraction, true);

            if (ParentModel.HasBeenDiscarded || interaction == null) return;

            _interactionStartReason = startReason;
            
            if (!NpcInInteractState) {
                CurrentInteraction = null;
                return;
            }
            
            CurrentInteraction = interaction;

            if (CurrentInteraction.GetInteractionPosition(Npc) is { } position 
                && !position.EqualsApproximately(Npc.Coords, SnapToInteractionRange)
                && !NpcTeleporter.TryToTeleport(Npc, position, TeleportContext.Interaction)
               ) {
                _walkingToPosition = true;
                _keepPosition.UpdatePlace(position, SnapToInteractionRange);
                _state = State.GoToInteraction;
                Movement.Controller.ToggleIdleOnlyRichAIActivity(true);
                Movement.ChangeMainState(_keepPosition);
            } else {
                TryStartInteraction().Forget();
            }
        }

        void OnPositionReached() {
            if (_walkingToPosition) {
                TryStartInteraction().Forget();
            }
        }

        async UniTaskVoid TryStartInteraction() {
            while (_stoppingInteraction?.IsStopping(ParentModel) ?? false) {
                if (!await AsyncUtil.DelayFrame(this, 1)) {
                    return;
                }
            }

            _stoppingInteraction = null;
            _walkingToPosition = false;
            
            InteractionChangedInfo.ChangeType changeType;
            if (_interactionStartReason == InteractionStartReason.ResumeInteraction) {
                ResumeInteraction();
                changeType = InteractionChangedInfo.ChangeType.Resume;
            } else {
                StartInteraction();
                changeType = InteractionChangedInfo.ChangeType.Start;
            }
            Npc.Trigger(Events.InteractionChanged, new InteractionChangedInfo(CurrentInteraction, changeType));
            
            WaitForFullyEntered().Forget();
        }

        void StartInteraction() {
            NpcHistorian.NotifyInteractions(Npc, $"Start Interaction:\n{CurrentInteraction}");
            _state = State.InteractPrepare;
            Movement.ResetMainState(_keepPosition);
            if (CurrentInteraction == null) {
                throw new InvalidOperationException("CurrentInteraction is null");
            }
            CurrentInteraction.StartInteraction(Npc, _interactionStartReason);
        }

        void ResumeInteraction() {
            NpcHistorian.NotifyInteractions(Npc, $"Resume Interaction:\n{CurrentInteraction}");
            _state = State.InteractPrepare;
            Movement.ResetMainState(_keepPosition);
            if (CurrentInteraction != null) {
                CurrentInteraction.ResumeInteraction(Npc, _interactionStartReason);
            }
        }

        async UniTaskVoid WaitForFullyEntered() {
            _interactPrepareToken?.Cancel();
            var token = new CancellationTokenSource();
            _interactPrepareToken = token;

            bool waitedSuccessfully = await AsyncUtil.WaitUntil(ParentModel, () => CurrentInteraction is { FullyEntered: true }, token.Token);

            if (!waitedSuccessfully || token.IsCancellationRequested) {
                return;
            }

            MarkFullyEntered();
        }

        void MarkFullyEntered() {
            if (_state is State.InteractPrepare) {
                _state = State.InteractFully;
                Npc.Trigger(Events.CurrentInteractionFullyEntered, CurrentInteraction);
            }
        }
        
        public void TryStop(INpcInteraction interaction, InteractionStopReason reason, bool onlyPause) {
            if (CurrentInteraction == interaction) {
                Stop(reason, onlyPause);
            }
        }
        
        public void Stop(InteractionStopReason reason, bool onlyPause) {
            if (CurrentInteraction == null) {
                return;
            }
            
            if (_state == State.NotInteracting) {
                CurrentInteraction = null;
                return;
            }
            
            if (_state == State.InteractPrepare) {
                _interactPrepareToken?.Cancel();
                _interactPrepareToken = null;
                MarkFullyEntered();
            }

            if (_state == State.InteractFully) {
                _stoppingInteraction = CurrentInteraction;
                if (onlyPause) {
                    CurrentInteraction.PauseInteraction(Npc, reason);
                    Npc.Trigger(Events.InteractionChanged, new InteractionChangedInfo(CurrentInteraction,
                        InteractionChangedInfo.ChangeType.Pause));
                } else {
                    CurrentInteraction.StopInteraction(Npc, reason);
                    CurrentInteraction = null;
                    Npc.Trigger(Events.InteractionChanged, new InteractionChangedInfo(CurrentInteraction,
                        InteractionChangedInfo.ChangeType.Stop));
                }
            } else {
                CurrentInteraction = null;
                Movement?.ResetMainState(_keepPosition);
            }
            
            _state = State.NotInteracting;
        }

        enum State : byte {
            GoToInteraction,
            InteractPrepare,
            InteractFully,
            NotInteracting,
        }

        public struct InteractionChangedInfo {
            public INpcInteraction interaction;
            public ChangeType changeType;
            
            public enum ChangeType : byte {
                Start,
                Resume,
                Stop,
                Pause
            }
            
            public InteractionChangedInfo(INpcInteraction interaction, ChangeType changeType) {
                this.interaction = interaction;
                this.changeType = changeType;
            }
        }
    }
}