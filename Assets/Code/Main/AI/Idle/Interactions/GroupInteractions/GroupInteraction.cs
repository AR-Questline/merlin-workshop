using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [RequireComponent(typeof(SphereCollider))]
    public class GroupInteraction : MonoBehaviour, INpcInteractionForwarder {
        protected const string AvailabilityGroup = "Availability";
        protected const string WeightGroup =AvailabilityGroup + "/Weight";
        protected const string RequirementsGroup = "Requirements";
        protected const string InteractingGroup = "Interacting";

        [FoldoutGroup(AvailabilityGroup, Expanded = true), SerializeField, InlineProperty, HideLabel] 
        FlagLogic availability;
        [FoldoutGroup(AvailabilityGroup, Expanded = true), ValidateInput(nameof(ValidateAllNpcsRequired), ValidateAllNpcsRequiredMsg), SerializeField, Tooltip("If only part of NPCs is present, should this interaction be able to be find by them and started")]
        protected bool allNpcsRequiredToFind = true;
        [FoldoutGroup(AvailabilityGroup, Expanded = true), SerializeField, Tooltip("Should requirements setup here be used for searching the interaction")]
        bool useMainInteractionsRequirements = false;
        [FoldoutGroup(AvailabilityGroup, Expanded = true), SerializeField, Tooltip("Should requirements setup in partial interactions be used for searching the interaction")]
        bool usePartialInteractionsRequirements = false;
        [FoldoutGroup(AvailabilityGroup, Expanded = true), SerializeField, Tooltip("Should partial interaction be actor specific")] 
        bool useActorInteractionsRequirements = true;
        [FoldoutGroup(WeightGroup), SerializeField, Tooltip("Should the search interaction weight be overriden")]
        bool overridePartialInteractionWeight = false;
        [FoldoutGroup(WeightGroup), ShowIf(nameof(overridePartialInteractionWeight)), SerializeField] 
        int overridenWeight;
        [FoldoutGroup(WeightGroup), SerializeField, Tooltip("Should this interaction be always performed if was able to be found")]
        bool forceThisInteractionWhenSearching;

        [ShowIf(nameof(useMainInteractionsRequirements)), FoldoutGroup(RequirementsGroup, Expanded = true), SerializeField, Tags(TagsCategory.InteractionID)] 
        string uniqueID;
        [ShowIf(nameof(UseMainAndNotUnique)), FoldoutGroup(RequirementsGroup, Expanded = true), SerializeField, Tags(TagsCategory.Interaction)] 
        public string[] tags = Array.Empty<string>();
        
        [FoldoutGroup(InteractingGroup, Expanded = true), SerializeField]
        protected ActorData[] actors = Array.Empty<ActorData>();
        [FoldoutGroup(InteractingGroup, Expanded = true), SerializeField, Tooltip("Should all partial interaction be stopped when one interaction is stopped")]
        bool stopAllWhenOneIsStopped = false;
        [FoldoutGroup(InteractingGroup, Expanded = true), ValidateInput(nameof(ValidateAllNpcsRequired), ValidateAllNpcsRequiredMsg), SerializeField, Tooltip("When all interaction have their NPC and first NPC starts interaction, should all other NPC be forced to drop what they're doing and join the interaction")]
        bool startAllWhenOneIsStarted = true;
        [FoldoutGroup(InteractingGroup, Expanded = true), SerializeField, Tooltip("Should the partial interaction start at the same moment. Before synchronize start the wait interaction is used")]
        bool synchronizeStart = false;
        [FoldoutGroup(InteractingGroup, Expanded = true), SerializeField, Tooltip("If partial interaction in not allowing for interrupt (eg. while talking), should ve allow interruption or not")]
        bool allowInterruptDuringTalk;
        [FoldoutGroup(InteractingGroup, Expanded = true), SerializeField, ShowIf(nameof(allowInterruptDuringTalk)), Tooltip("When trying to interrupt should we do it immediately (force it) or wait for special node in the story")]
        bool forceInterruptInsteadOfRequest;

        protected TargetState _targetState = TargetState.Inactive;
        protected TargetState? _previousState = null;
        protected Activity _activity = Activity.Inactive;

        bool UseMainAndNotUnique => useMainInteractionsRequirements && uniqueID.IsNullOrWhitespace();
        public virtual bool AllowDialogueAction => true;
        public bool IsAvailable => availability.Get(true);
        public bool IsUnique => !uniqueID.IsNullOrWhitespace() && useMainInteractionsRequirements;
        public bool UsePartialInteractionsRequirements => usePartialInteractionsRequirements;
        [UnityEngine.Scripting.Preserve] public bool UseMainInteractionsRequirements => useMainInteractionsRequirements;
        public bool AllowInterruptDuringTalk => allowInterruptDuringTalk;
        public bool ForceInterruptInsteadOfRequest => forceInterruptInsteadOfRequest;
        public int Priority => 1;

        const string ValidateAllNpcsRequiredMsg = "If not all Npcs are required to find this interaction, we can't use start all";
        bool ValidateAllNpcsRequired() {
            if (!allNpcsRequiredToFind && startAllWhenOneIsStarted) return false;
            return true;
        }
        
        void Awake() {
            foreach (var actor in actors) {
                if (actor.interaction is {} interaction) {
                    ModifyInteraction(interaction);
                }
                if (actor.waitInteraction is {} waitInteraction) {
                    ModifyInteraction(waitInteraction);
                }
            }
        }

        void ModifyInteraction(NpcInteraction interaction) {
            interaction.gameObject.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = false);
            interaction.SetUniqueId(null);
            interaction.OverrideWeight(overridePartialInteractionWeight ? overridenWeight : null, forceThisInteractionWhenSearching);
        }
        
        protected virtual void OnEnable() {
            gameObject.layer = RenderLayers.AIInteractions;
            if (!IsUnique) return;
            World.Services.Get<InteractionProvider>().TryRegisterUniqueSearchable(uniqueID, this);
        }

        protected virtual void OnDisable() {
            if (!IsUnique) return;
            World.Services.Get<InteractionProvider>().UnregisterUniqueSearchable(uniqueID);
        }
        
        /// <summary>
        /// Checks if this interaction is available AND checks if all npcs are ready to perform this interaction
        /// If there are missing empty Actor slots it tries to find NPCs to fill them.
        /// </summary>
        public virtual bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            if (!AvailableForNpc(npc, finder)) {
                return false;
            }
            
            bool npcIsInActorData = NpcIsInActorData(npc);
            
            if (startAllWhenOneIsStarted && _targetState is not TargetState.Inactive) {
                //If it's already active do a simple check to find if a NPC should join this interaction
                return npcIsInActorData;
            }

            if (_targetState is TargetState.Inactive && !AllActorsHaveSameActivity(Activity.Inactive)) {
                //if it's inactive and not all actors are in inactive state we need to wait until all npcs stop their interactions
                return false;
            }
            
            if (!npcIsInActorData) {
                //npc is not in actor data so we search for a correct actor slot to place it in.
                bool validNpc = false;
                for (int i = 0; i < actors.Length; i++) {
                    if (actors[i].activity is not Activity.Inactive) {
                        //This actor slot is already performing an interaction and shouldn't be replaced with another
                        continue;
                    }

                    if (usePartialInteractionsRequirements && !actors[i].Interaction(this).AvailableFor(npc, finder)) {
                        //This actor slot has interaction that can't be performed by this NPC
                        continue;
                    }

                    ValidateAssignedNpc(ref actors[i]);
                    if (useActorInteractionsRequirements) {
                        validNpc = actors[i].assignedIWithActor == null && actors[i].MatchActor(npc);
                    } else {
                        validNpc = actors[i].assignedIWithActor == null;
                    }

                    if (validNpc) {
                        actors[i].assignedIWithActor = npc;
                        break;
                    }
                }
                
                if (!validNpc) {
                    //NPC haven't found correct actor slot
                    return false;
                }
            }

            if (OtherNpcsAvailable(npc)) {
                //If all NPCs can start interaction we should start it
                ChangeTargetState(synchronizeStart ? TargetState.Waiting : TargetState.Active);
                return true;
            }
            //If NPCs is in actor slot but other NPCs can't start this interaction, this npc shouldn't start this interaction right now. 
            return false;
        }
        
        /// <summary>
        /// Checks if this interaction can be found by specific finder
        /// </summary>
        public bool AvailableForNpc(NpcElement npc, IInteractionFinder finder) {
            if (!IsAvailable) {
                return false;
            }

            if (!useMainInteractionsRequirements) {
                return true;
            }

            if (IsUnique) {
                return finder is InteractionUniqueFinder uniqueFinder
                    && InteractionUtils.AreSearchablesTheSameInteraction(npc, uniqueFinder.Searchable, this);
            }
            
            return finder is InteractionBaseFinder baseFinder
                && TagUtils.HasRequiredTag(tags, baseFinder.Tag);
        }
        
        /// <summary>
        /// Checks if all slots are occupied by NPCs, if not tries to fill them.
        /// </summary>
        bool OtherNpcsAvailable(NpcElement npcToExclude) {
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i].activity is not Activity.Inactive) {
                    //this actor is already performing this interaction
                    continue;
                }
                if (actors[i].NPC == npcToExclude) {
                    //this npc started this check and shouldn't be checked again
                    continue;
                }

                ValidateAssignedNpc(ref actors[i]);
                actors[i].assignedIWithActor ??= FindValidNpc(ref actors[i], npcToExclude);
                var npc = actors[i].NPC;
                if (npc == null && allNpcsRequiredToFind) {
                    return false;
                }
            }
            return true;
        }

        void ValidateAssignedNpc(ref ActorData data) {
            if (data.NPC is { } assignedNpc) {
                bool stillValid = assignedNpc is { HasBeenDiscarded: false, Behaviours: not null } && assignedNpc.Behaviours!.CanPerformInteraction(data.Interaction(this), Priority);
                if (!stillValid) {
                    data.assignedIWithActor = null;
                }
            }
        }

        /// <summary>
        /// Searches for a NPC to fill an empty slot
        /// </summary>
        NpcElement FindValidNpc(ref ActorData data, NpcElement npcToExclude) {
            if (!useActorInteractionsRequirements) {
                //TODO Maybe some optimized search for nearby available NPCs will be implemented here someday
                return null;
            }

            var validLocations = StoryUtils.MatchActorLocations(data.actor.Get());
            foreach (var l in validLocations) {
                var npc = l.TryGetElement<NpcElement>();
                if (npc == npcToExclude || npc == null || NpcIsInActorData(npc)) {
                    continue;
                }
                if (!npc.Behaviours.CanPerformInteraction(data.Interaction(this), Priority)) {
                    //Check if NPCs Idle State can find this interaction
                    continue;
                }
                return npc;
            }
            
            return null;
        }

        void StartAllInteractions(NpcElement npcToExclude, InteractionStartReason startReason) {
            ChangeMainActivity(Activity.ChangingState);
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i].NPC == npcToExclude || actors[i].activity is Activity.ChangingState) {
                    continue;
                }
                StartInteraction(ref actors[i], startReason);
            }
        }

        void StartInteraction(ref ActorData data, InteractionStartReason startReason) {
            var interaction = _targetState is TargetState.Waiting ? data.WaitInteraction(this) : data.Interaction(this);
            if (data.instantSwapFromWaitToInteract) {
                bool previousValid = _previousState is TargetState.Inactive or TargetState.Waiting;
                if (previousValid && _targetState is TargetState.Active) {
                    startReason = InteractionStartReason.InteractionFastSwap;
                }
            }

            var npc = data.NPC;
            var behaviours = npc?.Behaviours;
            if (behaviours == null || interaction == behaviours.CurrentMainInteraction) {
                return;
            }
            
            ChangeNpcActivity(ref data, Activity.ChangingState);
            behaviours.PerformSpecificInteraction(interaction, startReason);
                
            AddIdleDataChangedListener(ref data);
        }

        void StopAllInteractions(NpcElement npcToExclude) {
            if (_targetState is TargetState.Inactive) {
                return;
            }
            ChangeTargetState(TargetState.Inactive);
            ChangeMainActivity(Activity.Disabling);
            
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i].NPC == npcToExclude || actors[i].activity is Activity.Inactive or Activity.Disabling) {
                    continue;
                }
                
                StopInteraction(ref actors[i]);
            }
        }

        void StopInteraction(ref ActorData data) {
            ChangeNpcActivity(ref data, Activity.Disabling);
            data.NPC?.Behaviours.PerformSpecificInteraction(null);
        }
        
        void OnIdleDataChanged(NpcElement npc) {
            ref var data = ref GetActorData(npc);
            RemoveIdleDataChangedListener(ref data);
            data.activity = Activity.Inactive;
        }

        void AddIdleDataChangedListener(ref ActorData data) {
            var npc = data.NPC;
            var behaviours = npc.Behaviours;
            if (data.idleDataChangedListener != null) {
                World.EventSystem.RemoveListener(data.idleDataChangedListener);
                data.idleDataChangedListener = null;
            }
            data.idleDataChangedListener = behaviours.Location.ListenTo(IIdleDataSource.Events.InteractionIntervalChanged, 
                () => OnIdleDataChanged(npc), behaviours);

        }

        void RemoveIdleDataChangedListener(ref ActorData data) {
            if (data.idleDataChangedListener != null) {
                World.EventSystem.RemoveListener(data.idleDataChangedListener);
                data.idleDataChangedListener = null;
            }
        }

        public virtual void OnInteractionBooked(NpcElement npc, INpcInteraction interaction) {
            if (_targetState is TargetState.Inactive) {
                //If target state is inactive we shouldn't start any interactions. Probably some kind of race conditions happened.
                StopInteraction(ref GetActorData(npc));
                return;
            }
            
            ChangeNpcActivity(npc, Activity.ChangingState);
            if (startAllWhenOneIsStarted && _activity is Activity.Inactive) {
                //We should start all interactions only once (so Activity is still Inactive)
                StartAllInteractions(npc, InteractionStartReason.ChangeInteraction);
            }
        }

        public virtual void OnInteractionUnbooked(NpcElement npc, INpcInteraction interaction) {
            if (synchronizeStart && _activity is Activity.ChangingState && _targetState is TargetState.Active) {
                //The only unbooking allowed is the change from Waiting to Active with the synchronized start
                return;
            }
            
            ChangeNpcActivity(npc, Activity.Inactive);
            RemoveIdleDataChangedListener(ref GetActorData(npc));
            
            if (AllActorsHaveSameActivity(Activity.Inactive)) {
                ChangeTargetState(TargetState.Inactive);
                ChangeMainActivity(Activity.Inactive);
                return;
            }
            
            if (stopAllWhenOneIsStopped) {
                StopAllInteractions(npc);
            } else {
                ChangeMainActivity(Activity.ChangingState);
            }
        }
        
        public virtual void OnInteractionStarted(NpcElement npc, INpcInteraction interaction, InteractionStartReason startReason) {
            if (_targetState is TargetState.Inactive) {
                //If interaction was resumed with this conditions, it means this interaction should be stopped before but it was paused.
                StopInteraction(ref GetActorData(npc));
                return;
            }
            
            ChangeNpcActivity(npc, Activity.InState);
            if (_targetState is TargetState.Waiting) {
                //If everyone is instate waiting it means they can finally start the main interaction.
                if (AllActorsHaveSameActivity(Activity.InState)) {
                    ChangeTargetState(TargetState.Active);
                    StartAllInteractions(null, startReason);
                }
                return;
            }
            
            if (AllActorsHaveSameActivity(Activity.InState)) {
                ChangeMainActivity(Activity.InState);
            }
        }
        
        public virtual void OnInteractionResumed(NpcElement npc, INpcInteraction interaction, InteractionStartReason startReason) {
            ref var data = ref GetActorData(npc);
            if (data.activity is Activity.ChangingState) {
                //First Interaction In Lifetime is "resumed" instead of "started" so we need to double check it's real resume 
                OnInteractionStarted(npc, interaction, startReason);
                return;
            }
            
            if (stopAllWhenOneIsStopped || _targetState is TargetState.Inactive) {
                //If interaction was resumed with this conditions, it means this interaction should be stopped before but it was paused.
                StopInteraction(ref data);
                return;
            }

            ChangeNpcActivity(npc, Activity.InState);
            if (AllActorsHaveSameActivity(Activity.InState)) {
                ChangeMainActivity(Activity.InState);
            }
        }

        public virtual void OnInteractionStopped(NpcElement npc, INpcInteraction interaction) {
            InteractionStoppedInternal(npc, Activity.Disabling);
        }

        public virtual void OnInteractionPaused(NpcElement npc, INpcInteraction interaction) {
            InteractionStoppedInternal(npc, Activity.Paused);
        }

        void InteractionStoppedInternal(NpcElement npc, Activity newActivity) {
            ChangeNpcActivity(npc, newActivity);
            if (stopAllWhenOneIsStopped && !(synchronizeStart && _targetState is TargetState.Active && _activity is Activity.ChangingState)) {
                //We need to exclude current NPC because it's interaction is paused or already stopped
                //if paused it means other interaction (like talking) is higher up in the stack and this interaction will be stopped after it is resumed.
                StopAllInteractions(npc);
            } else {
                ChangeMainActivity(Activity.ChangingState);
            }
        }
        
        public virtual void OnTalkStarted(Story story, NpcElement npc, INpcInteraction interaction) { }
        public virtual void OnEndTalk(NpcElement npc, INpcInteraction interaction) { }

        public INpcInteraction GetInteraction(NpcElement npc) {
            ref var data = ref GetActorData(npc); 
            if (synchronizeStart && _targetState is TargetState.Waiting) {
                return data.WaitInteraction(this);
            }
            return data.Interaction(this);
        }

        public void GetAllInteractions(List<INpcInteraction> interactions) {
            for (int i = 0; i < actors.Length; i++) {
                interactions.Add(actors[i].Interaction(this));
                interactions.Add(actors[i].WaitInteraction(this));
            }
        }

        void ChangeTargetState(TargetState state) {
            _previousState = _targetState;
            _targetState = state;
        }

        void ChangeMainActivity(Activity activity) {
            _activity = activity;
            OnMainActivityChanged(activity);
        }
        
        protected virtual void OnMainActivityChanged(Activity activity) {}
        
        void ChangeNpcActivity(NpcElement npc, Activity activity) {
            ChangeNpcActivity(ref GetActorData(npc), activity);
        }

        protected void ChangeNpcActivity(ref ActorData data, Activity activity) {
            data.activity = activity;
        }

        ref ActorData GetActorData(NpcElement npc) {
            for (int i = 0; i < actors.Length; i++) {
                var actor = actors[i];
                if (actor.MatchNpc(npc) || (!actor.HasNpcAssigned && (!useActorInteractionsRequirements || actor.MatchActor(npc)))) {
                    return ref actors[i];
                }
            }
            string actorsInfo = string.Join("\n", actors.Select(a => $"Actor: {(a.actor.IsEmpty ? "none" : a.actor.Get().Id)} = Npc: {(a.NPC?.Name ?? "null")}"));
            throw new Exception($"{npc} is not in actor data for {this}\n{actorsInfo}");
        }

        bool NpcIsInActorData(NpcElement npc) {
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i].MatchNpc(npc)) {
                    return true;
                }
            }
            return false;
        }

        public bool AllActorsHaveSameActivity(Activity activity) {
            return actors.All(a => a.activity == activity);
        }
        
#if UNITY_EDITOR
        void Reset() {
            gameObject.layer = RenderLayers.AIInteractions;
        }
#endif

        [Serializable]
        protected struct ActorData {
            public ActorRef actor;
            [HideIf(nameof(PositionSet))] public NpcInteraction interaction;
            [HideIf(nameof(InteractionSet))] public Transform position;
            public NpcInteraction waitInteraction;
            [ShowIf(nameof(WaitInteractionSet))]
            public bool instantSwapFromWaitToInteract;
            
            [HideInInspector] public GroupInteractionPart groupInteraction;
            [HideInInspector] public GroupInteractionPart groupWaitInteraction;
            [HideInInspector] [UnityEngine.Scripting.Preserve] public GroundedPosition lookAt;
            [HideInInspector] public ILocationElementWithActor assignedIWithActor;
            [HideInInspector] [UnityEngine.Scripting.Preserve] public bool came;
            [HideInInspector] public Activity activity;
            [HideInInspector] public IEventListener idleDataChangedListener;

            public INpcInteraction Interaction(GroupInteraction parent) => groupInteraction ??= CreateInteraction(parent);
            public INpcInteraction WaitInteraction(GroupInteraction parent) => groupWaitInteraction ??= CreateWaitInteraction(parent);
            [CanBeNull] public readonly NpcElement NPC => assignedIWithActor as NpcElement;
            public readonly bool HasNpcAssigned => assignedIWithActor != null;
            public readonly bool MatchActor(NpcElement npc) => actor.guid == npc.Actor.Id;
            public readonly bool MatchNpc(NpcElement npc) => NPC == npc;
            public readonly bool MatchAssignedNpc(NpcElement npc) => assignedIWithActor == npc;
            public readonly bool InteractionSet => interaction != null;
            public readonly bool PositionSet => position != null;
            readonly bool WaitInteractionSet => waitInteraction != null;

            GroupInteractionPart CreateInteraction(GroupInteraction parent) {
                if (InteractionSet) {
                    return new GroupInteractionPart(parent, interaction);
                } else {
                    Vector3 pos = PositionSet ? position.position : parent.transform.position;
                    Vector3 forw = PositionSet ? position.forward : parent.transform.forward;
                    var standInteraction = new StandInteraction(IdlePosition.World(pos), IdlePosition.World(forw), null);
                    return new GroupInteractionPart(parent, standInteraction);
                }
            }
            
            GroupInteractionPart CreateWaitInteraction(GroupInteraction parent) {
                if (waitInteraction != null) {
                    return new GroupInteractionPart(parent, waitInteraction);
                }
                Vector3 pos;
                Vector3 forw;
                if (InteractionSet && interaction.GetInteractionPosition(null).HasValue) {
                    pos = interaction.GetInteractionPosition(null).Value;
                    forw = interaction.GetInteractionForward(null);
                } else {
                    pos = PositionSet ? position.position : parent.transform.position;
                    forw = PositionSet ? position.forward : parent.transform.forward;
                }

                var standInteraction = new StandInteraction(IdlePosition.World(pos), IdlePosition.World(forw), null);
                return new GroupInteractionPart(parent, standInteraction);
            }
        }

        public enum Activity : byte {
            Inactive,
            Disabling,
            ChangingState,
            InState,
            Paused
        }

        public enum TargetState : byte {
            Inactive,
            Waiting,
            Active
        }
    }
}
