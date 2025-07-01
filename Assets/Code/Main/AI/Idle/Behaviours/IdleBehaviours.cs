#if DEBUG && !NPC_LOGIC_DEBUGGING
#define NPC_LOGIC_DEBUGGING
#endif

using System;
using System.Diagnostics;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Idle.Interactions.Saving;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.AI.Idle.Behaviours {
    public sealed partial class IdleBehaviours : Element<NpcElement> {
        public override ushort TypeForSerialization => SavedModels.IdleBehaviours;

        [UnityEngine.Scripting.Preserve] bool _initialized;
        
        TalkInteraction _talk;
        CommuteToInteraction _commuteTo;
        RotateToInteraction _rotateTo;

        RefreshParams _refreshParams;
        IdleStack _idleStack;

        public bool DEBUG_Disable;

        public bool Active => Npc.NpcAI is { Working: true };
        public NpcElement Npc => ParentModel;
        public Location Location => Npc.ParentModel;
        NpcInteractor Interactor => Npc.Interactor;
        bool CanInteract => Npc is { Movement: not null, Interactor: not null };
        [CanBeNull] IIdleDataSource IdleDataSource => Location.Elements<IIdleDataSource>().MaxBy(source => source.Priority);
        public INpcInteraction CurrentInteraction => Interactor.CurrentInteraction;
        public INpcInteraction CurrentUnwrappedInteraction => InteractionUtils.GetUnwrappedInteraction(Interactor.CurrentInteraction);
        public INpcInteraction CurrentMainInteraction => _idleStack.Peek();
        public IInteractionFinder CurrentFinder => _idleStack.Source?.Finder;
        public bool HasAnchor => _idleStack.HasAnchor;
        public float PositionRange => IdleDataSource?.PositionRange ?? 0.8f;

        protected override void OnInitialize() {
            _idleStack = new IdleStack(this);

            Location.ListenTo(IIdleDataSource.Events.InteractionIntervalChanged, OnIntervalChange, this);
            Location.ListenTo(IIdleDataSource.Events.InteractionOneShotTriggered, OnOneShotTriggered, this);
            Npc.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, this);
            Npc.ListenTo(UnconsciousElement.Events.LoseConscious, _ => _idleStack.Drop(null, InteractionStopReason.StoppedIdlingInstant).Forget(), this);
            Npc.ListenTo(UnconsciousElement.Events.RegainConscious, _ => RefreshCurrentBehaviour(startReason: InteractionStartReason.NPCActivated), this);
            Npc.ListenTo(NpcElement.Events.PresenceChanged, _ => RefreshCurrentBehaviour(startReason: InteractionStartReason.NPCActivated), this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            try {
                Interactor.Stop(InteractionStopReason.Death, false);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void OnNpcVisualInitialize() {
            _initialized = true;
            if (LoadingScreenUI.IsFullyLoading && !LoadSystem.IsLoadingDifferentVersion) {
                RefreshCurrentBehaviour(startReason: InteractionStartReason.NPCReactivatedFromGameLoad);
            } else {
                RefreshCurrentBehaviour(startReason: InteractionStartReason.NPCActivated);
            }
        }

        void OnStateChanged(Change<IState> change) {
            if (!Npc.HasCompletelyInitialized) {
                //Ignore first events because they are sent before IdleBehaviours has benn fully initialized
                return;
            }
            
            //If AI/Idle is enabled it will start interactions BUT it won't force drop current interaction
            if (change is (_, StateAIWorking)) {
                RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCActivated);
                return;
            }

            //NpcInvolvement will handle this
            if (_talk != null) {
                return;
            }
            
            //If AI/Idle is disabled it will stop interactions
            if (change is (StateAIWorking, _)) {
                if (change is (_, StateAIPaused)) {
                    OnNpcPresenceDisabled().Forget();
                } else {
                    RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCDeactivated);
                }
                return;
            }

            if (change is (_, StateCombat) or (_, StateAlert)) {
                RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCStartedCombat);
                return;
            }
            
            if (change is (StateCombat, _) or (StateAlert, _)) {
                RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCEndedCombat);
                return;
            }
            
            if (change is (StateIdle, not null)) {
                RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCDeactivated);
                return;
            }
            
            if (change is (not null, StateIdle)) {
                RefreshCurrentBehaviour(false, startReason: InteractionStartReason.NPCActivated);
                return;
            }
        }

        public async UniTask StartTalk(Story story, bool rotateToHero, bool forceExitInteraction = false) {
            if (Npc.NpcAI is { InCombat: true }) {
                return;
            }

            if (!Active) {
                await TalkInCurrentInteraction();
                return;
            }
            
            var shouldStartTalkInteraction = forceExitInteraction ||
                                  Interactor.IsGoingToInteraction ||
                                  CurrentInteraction == null ||
                                  !CurrentInteraction.TryStartTalk(story, Npc, rotateToHero); // <- Side effect
            if (shouldStartTalkInteraction) {
                await TalkAsNewInteraction(rotateToHero);
            } else {
                await TalkInCurrentInteraction();
            }
        }

        async UniTask TalkInCurrentInteraction() {
            //If IdleStack is dropping or waits for first interaction we cant modify current interaction
            while (_idleStack.IsDropping || _idleStack.IsCurrentStackEmpty) {
                if (!await AsyncUtil.DelayFrame(this, 1)) {
                    return;
                }
            }
            _idleStack.SetAsAnchor(true);
        }

        async UniTask TalkAsNewInteraction(bool rotateToHero) {
            _talk ??= new TalkInteraction(rotateToHero);
            PushAnchorToStack(_talk);
            // If CanBeInterrupted than it has not been started yet or was ended. We wait for its start.
            while (_talk is { CanBeInterrupted: true} ) {
                if (!await AsyncUtil.DelayFrame(this, 1)) {
                    return;
                }
            }
        }

        public async UniTask EndTalk(bool rotReturnToInteraction) {
            if (_talk != null) {
                await DropAnchor();
                _talk = null;
            } else {
                await DropToAnchor();
                _idleStack.SetAsAnchor(false);
            }

            if (CurrentInteraction == null) {
                _idleStack.Peek()?.EndTalk(Npc, rotReturnToInteraction);
            } else {
                CurrentInteraction.EndTalk(Npc, rotReturnToInteraction);
            }
        }
        
        public void AddOverride(DeterministicInteractionFinder finder, StoryBookmark callback, bool forgetOnSceneChange = false, InteractionStartReason? overridenStartReason = null) {
            AddOverride(new InteractionOverride(finder, callback, forgetOnSceneChange: forgetOnSceneChange, overridenStartReason: overridenStartReason));
        }
        
        public void AddOverride(InteractionOverride interactionOverride) {
            AddInteractionSpecificSource(interactionOverride);
        }

        public void AddInteractionSpecificSource(InteractionSceneSpecificSource interactionSceneSpecificSource) {
            if (interactionSceneSpecificSource.OverridenStartReason.HasValue) {
                _refreshParams.startReason = interactionSceneSpecificSource.OverridenStartReason.Value;
            }
            UpdateStackSource(AddElement(interactionSceneSpecificSource));
        }

        public InteractionBookingResult Book(INpcInteraction interaction) {
            return _idleStack.Book(interaction);
        }
        
        void UpdateStackSource(IInteractionSource source) {
            if (!CanInteract) {
                RefreshCurrentBehaviour(true);
                return;
            }
            
            if (source.Equals(_idleStack.Source)) {
                if (_idleStack.IsCurrentStackEmpty) {
                    if (_idleStack.IsWaitingStackEmpty) {
                        AddStackSourceInteractions(source);
                        return;
                    }
                    RefreshCurrentBehaviour(_refreshParams.forceDrop, _refreshParams.startReason);
                    return;
                }
                
                // if both Finders are ChangeSceneFinders NPC should stay in abyss
                if (source.Finder is InteractionChangeSceneFinder) {
                    _refreshParams = default;
                    return;
                }
                
                // pause interaction if the AI was deactivated
                if (_refreshParams.startReason == InteractionStartReason.NPCDeactivated) {
                    Interactor.Stop(GetStopReason(_refreshParams.startReason), true);
                    return;
                }

                // no need to drop if we are using the same source
                if (!_refreshParams.forceDrop && _idleStack.TryToPerformAgain(_refreshParams.startReason)) {
                    _refreshParams = default;
                    return;
                }
            }

            if (_idleStack.TryDrop(source, _refreshParams.forceDrop, GetStopReason(_refreshParams.startReason))) {
                AddStackSourceInteractions(source);
            }
        }

        static InteractionStopReason GetStopReason(InteractionStartReason startReason) {
            return startReason switch {
                InteractionStartReason.InteractionFastSwap => InteractionStopReason.StoppedIdlingInstant,
                InteractionStartReason.NPCActivated => InteractionStopReason.NPCReactivated,
                InteractionStartReason.NPCDeactivated => InteractionStopReason.NPCDeactivated,
                InteractionStartReason.NPCPresenceDisabled => InteractionStopReason.NPCPresenceDisabled,
                InteractionStartReason.NPCStartedCombat => InteractionStopReason.NPCStartedCombat,
                _ => InteractionStopReason.ChangeInteraction
            };
        }

        void AddStackSourceInteractions(IInteractionSource source) {
            var refreshParams = _refreshParams;
            _refreshParams = default;
            INpcInteraction interaction = null;
            SavedInteractionData savedInteractionData = null;
            
            if (refreshParams.startReason == InteractionStartReason.NPCReactivatedFromGameLoad) {
                savedInteractionData = ParentModel.SavedInteractionData;
                interaction = savedInteractionData?.TryToGetInteraction(this);
                interaction ??= source.Finder is InteractionBaseFinder baseFinder ? baseFinder.FindInteractionAfterLoad(this, null) : null;
            }

            if (interaction == null) {
                if (refreshParams.forcedInteraction != null &&
                    source.Finder.CanFindInteraction(this, refreshParams.forcedInteraction, false)) {
                    interaction = refreshParams.forcedInteraction;
                    refreshParams.forcedInteraction = null;
                } else {
                    interaction = source.Finder.FindInteraction(this) ?? source.GetFallbackInteraction(this, IdleDataSource);
                }
            }

            refreshParams.forcedInteraction = null;
            var tempInteractions = SelectTemporaryInteraction(interaction, source.Finder, refreshParams.startReason, savedInteractionData);
            if (tempInteractions != null) {
                _idleStack.Push(interaction, false, reason: refreshParams.startReason);
                PushTemporaryInteractions(tempInteractions, refreshParams.startReason);
            } else {
                _idleStack.Push(interaction, reason: refreshParams.startReason);
            }
        }

        public void PushAnchorToStack(INpcInteraction interaction) {
            _idleStack.Push(interaction, isAnchor: true);
        }
        
        public void PushToStack(INpcInteraction interaction) {
            _idleStack.Push(interaction);
        }
        
        public void SetAsAnchor(bool active) {
            _idleStack.SetAsAnchor(active);
        }

        public async UniTask DropAnchor() {
            if (Npc.NpcAI.IsOrWillBeInNonPacifistState) {
                await _idleStack.DropAnchor(InteractionStopReason.NPCStartedCombat);
            } else {
                await _idleStack.DropAnchor();
            }
        }

        public async UniTask DropToAnchor() {
            if (Npc.NpcAI.IsOrWillBeInNonPacifistState) {
                await _idleStack.DropToAnchor(InteractionStopReason.NPCStartedCombat);
            } else {
                await _idleStack.DropToAnchor();
            }
        }

        void OnIntervalChange(IIdleDataSource data) {
            if (!CanInteract) {
                return;
            }

            if (data == null) { // NPC Presence was disabled
                OnNpcPresenceDisabled().Forget();
                return;
            }

            if (!data.IsFullyInitialized) {
                return;
            }
            
            if (Npc.NpcPresence == null || Npc.NpcPresence.AllowIntervalChange(data)) {
                RefreshCurrentBehaviour();
            }
        }
        
        void OnOneShotTriggered(InteractionOneShotData oneShot) {
            if (!CanInteract || !Active) {
                return;
            }
            UpdateStackSource(AddElement(new InteractionOneShot(!oneShot.canBePaused, oneShot.CreateFinder())));
        }

        async UniTaskVoid OnNpcPresenceDisabled() {
            _idleStack.Drop(null, InteractionStopReason.NPCPresenceDisabled).Forget();
            _talk = null;
            while (_idleStack.IsDropping) {
                await AsyncUtil.DelayFrame(this, 1);
            }

            if (HasBeenDiscarded || ParentModel == null) {
                return;
            }
            
            if (Npc.NpcPresence == null || Npc.NpcPresence.AllowIntervalChange(null)) {
                RefreshCurrentBehaviour(startReason: InteractionStartReason.NPCPresenceDisabled);
            }
        }

        public bool CanPerformInteraction(INpcInteraction interaction, int priorityOverride = -1, bool ignoreInteractionRequirements = false) {
            if (CurrentInteraction is { CanBeInterrupted: false }) {
                return false;
            }
            if (!_idleStack.Source?.Finder?.CanFindInteraction(this, interaction, ignoreInteractionRequirements) ?? true) {
                return false;
            }

            int priority = (priorityOverride == -1) ? interaction.Priority : priorityOverride;
            if (_idleStack.MaxPriority >= priority) {
                return false;
            }
            return true;
        }

        public void PerformSpecificInteraction(INpcInteraction interaction, InteractionStartReason startReason = InteractionStartReason.ChangeInteraction) {
            if (interaction == CurrentMainInteraction) {
                return;
            }
            _refreshParams.Append(new RefreshParams {
                forceDrop = true,
                startReason = startReason,
                forcedInteraction = interaction
            });
            if (interaction == null) {
                _refreshParams.forcedInteraction = null;
            }
            Services.Get<IdleBehavioursRefresher>().RequestRefresh(this);
        }
        
        public void RefreshCurrentBehaviour(bool forceDrop = false, InteractionStartReason startReason = InteractionStartReason.ChangeInteraction) {
            if (_refreshParams.startReason == InteractionStartReason.NPCReactivatedFromGameLoad) {
                startReason = InteractionStartReason.NPCReactivatedFromGameLoad;
            }
            _refreshParams.Append(new RefreshParams{
                forceDrop = forceDrop,
                startReason = startReason
            });
            Services.Get<IdleBehavioursRefresher>().RequestRefresh(this);
        }

        /// <summary> call only from IdleBehavioursRefresher </summary>
        public void InternalRefreshCurrentBehaviour() {
            if (HasBeenDiscarded) {
                return;
            }

#if NPC_LOGIC_DEBUGGING
            if (DEBUG_Disable) {
                StopInteractions();
                return;
            }
#endif
            if (Interactor.NpcInInteractState) {
                var source = TryGetElement<InteractionStoryBasedOverride>()
                             ?? TryGetElement<InteractionOverride>()
                             ?? TryGetElement<InteractionOneShot>()
                             ?? Npc.NpcPresence?.InteractionSource 
                             ?? IdleDataSource?.GetCurrentSource();

                if (source != null) {
                    UpdateStackSource(source);
                    return;
                }
            }

            StopInteractions();
        }

        void StopInteractions() {
            InteractionStopReason stopReason;
            bool onlyPause = true;
            switch (_refreshParams.startReason) {
                case InteractionStartReason.NPCReactivatedFromGameLoad:
                    stopReason = InteractionStopReason.NPCDeactivated;
                    if (Npc is { IsUnique: true, NpcPresence: null, Controller: not null }) {
                        Npc.Controller.MoveToAbyss();
                    }
                    break;
                case InteractionStartReason.NPCPresenceDisabled:
                    stopReason = InteractionStopReason.NPCPresenceDisabled;
                    break;
                case InteractionStartReason.NPCEndedCombat:
                case InteractionStartReason.NPCActivated:
                    stopReason = InteractionStopReason.NPCReactivated;
                    break;
                case InteractionStartReason.NPCDeactivated:
                    stopReason = InteractionStopReason.NPCDeactivated;
                    break;
                case InteractionStartReason.NPCStartedCombat:
                    stopReason = InteractionStopReason.NPCStartedCombat;
                    onlyPause = false;
                    break;
                default:
                    stopReason = InteractionStopReason.StoppedIdling;
                    break;
            }
            Interactor.Stop(stopReason, onlyPause);
            _refreshParams = default;
        }

        public void PushTemporaryInteractions(INpcInteraction[] tempInteractions, InteractionStartReason startReason) {
            int length = tempInteractions.Length;
            for (int i = 0; i < length; i++) {
                bool last = i == length - 1;
                _idleStack.Push(tempInteractions[i], last, reason: startReason);
            }
        }

        public INpcInteraction[] SelectTemporaryInteraction(INpcInteraction interaction, IInteractionFinder finder, InteractionStartReason startReason, SavedInteractionData savedInteractionData = null) {
            if (finder is InteractionFallbackFinder) {
                if (CurrentUnwrappedInteraction is FallbackInteraction fallback) {
                    return new [] { fallback };
                }

                Interactor.Stop(InteractionStopReason.MySceneUnloading, false);
            } else if (CurrentUnwrappedInteraction is ChangeSceneInteraction changeSceneInteraction) {
                if (finder is InteractionChangeSceneFinder changeSceneFinder) {
                    if (changeSceneInteraction.Scene == changeSceneFinder.Scene) {
                        return null;
                    }
                }

                Interactor.Stop(InteractionStopReason.ComebackFromScene, false);
            }

            if (finder is InteractionFakeDeathFinder) {
                return null;
            }

            bool shouldRotate = interaction != null;
            if (shouldRotate) {
                _rotateTo ??= new RotateToInteraction();
                _rotateTo.Setup(interaction.GetInteractionForward(Npc));
            }
            
            var radius = interaction != null ? NpcInteractor.SnapToInteractionRange : finder.GetInteractionRadius(this);
            float radiusSqr = radius * radius;

            Vector3 position;
            bool shouldEnterInteraction;
            if (savedInteractionData != null && !NpcPresence.InAbyss(Npc.Coords)) {
                position = Npc.Coords;
                shouldEnterInteraction = true;
            } else {
                position = interaction?.GetInteractionPosition(Npc) ?? finder.GetDesiredPosition(this);
                
                bool inInteractionRange = Vector3.SqrMagnitude(Npc.Coords - position) <= radiusSqr;
                shouldEnterInteraction = inInteractionRange 
                                         || (!_idleStack.IsDropping 
                                             && (startReason != InteractionStartReason.NPCReactivatedFromGameLoad || NpcPresence.InAbyss(Npc.Coords))
                                             && NpcTeleporter.TryToTeleport(Npc, position, TeleportContext.Interaction));
            }
            
            bool canEnterInteraction = Npc.NpcAI is { Working: true };
            if (shouldEnterInteraction && canEnterInteraction) {
                if (shouldRotate && !_rotateTo.InCorrectRotation(ParentModel)) {
                    return new INpcInteraction[] { _rotateTo };
                }
                return null;
            }
            
            _commuteTo ??= new CommuteToInteraction();
            _commuteTo.Setup(position, PositionRange, radiusSqr);
            if (shouldRotate) {
                return new INpcInteraction[] { _rotateTo, _commuteTo };
            }
            return new INpcInteraction[] { _commuteTo } ;
        }
        
        public SavedInteractionData TryGetSavedInteractionData() {
            var data = ParentModel.SavedInteractionData;
            ParentModel.SavedInteractionData = null;
            return data;
        }

        [Conditional("DEBUG")]
        public void NotifyHistorian(string message) {
            NpcHistorian.NotifyInteractions(Npc, message);
        }

        struct RefreshParams {
            public bool forceDrop;
            public InteractionStartReason startReason;
            public INpcInteraction forcedInteraction;

            public void Append(RefreshParams other) {
                forceDrop = forceDrop || other.forceDrop;
                startReason = other.startReason;
                if (other.forcedInteraction != null) {
                    forcedInteraction = other.forcedInteraction;
                }
            }
        }
    }
}