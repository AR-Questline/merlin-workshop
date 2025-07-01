using System;
using System.Linq;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [RequireComponent(typeof(SphereCollider))]
    public class StoryInteraction : GroupInteraction {
        const string AfterStoryGroup = InteractingGroup + "/AfterStory";
        const float EndStoryDistance = 50;
        const float EndStoryDistanceSq = EndStoryDistance * EndStoryDistance;
        
        /// <summary>
        /// To prevent discarding story after manually removing NPCs from story interaction
        /// </summary>
        public static bool allowDiscardingStory = true;
        
        [SerializeField, FoldoutGroup(InteractingGroup, 1)] 
        StoryBookmark bookmark;
        [SerializeField, FoldoutGroup(InteractingGroup)] 
        DialogueViewType dialogueViewType;
        [SerializeField, FoldoutGroup(InteractingGroup)] 
        Transform focusOverride;
        [SerializeField, FoldoutGroup(InteractingGroup)]
        float range;
        [SerializeField, FoldoutGroup(InteractingGroup), Tooltip("If Hero runs away from the Story Interaction, at what distance should it be stopped (Never is 50m, maximum value)")]
        HeroRequiredDialogueDistance stopStoryDistance = HeroRequiredDialogueDistance.Never;
        [SerializeField, FoldoutGroup(InteractingGroup), Tooltip("If Hero tries to talk to a NPCs in this interaction, should he able to talk to him")]
        bool allowInterruptWithHeroDialogueAction = true;
        [SerializeField, FoldoutGroup(InteractingGroup), Tooltip("If Hero is in combat, should the story be able to start")]
        bool startInCombat = true;
        [SerializeField, FoldoutGroup(InteractingGroup), ShowIf(nameof(allNpcsRequiredToFind)), Tooltip("If only part of NPCs is present, should the story be able to start")]
        bool requireAllNPCsToTriggerStory = true;
        [SerializeField, FoldoutGroup(InteractingGroup), Tooltip("If story is completed successfully could it be started again")]
        bool triggerOnlyOnce = true;
        [SerializeField ,FoldoutGroup(InteractingGroup), Tooltip("Should spine rotations from partial interaction be overriden")]
        bool overrideSpineRotation;
        [SerializeField, FoldoutGroup(InteractingGroup), ShowIf(nameof(overrideSpineRotation))] 
        SpineRotationType spineRotationType = SpineRotationType.FullRotation;
        [SerializeField, FoldoutGroup(AfterStoryGroup), ShowIf(nameof(triggerOnlyOnce)), Tooltip("If Story ends should NPCs still be able to find this interaction and perform it without triggering new story")]
        bool canBePerformedAfterStory;
        [SerializeField, FoldoutGroup(AfterStoryGroup), Tooltip("If Story ends should NPCs stay in the interaction (not leave)")]
        bool keepInInteractionAfterStory;

        Story _story;
        string _triggeredLabel;
        ContextualFacts _triggeredStoryFacts;
        NpcElement _interruptingNpc;

        public override bool AllowDialogueAction => allowInterruptWithHeroDialogueAction || _story == null;
        public StoryBookmark Bookmark => bookmark;
        public Story Story => _story;
        ContextualFacts TriggeredStoryFacts => _triggeredStoryFacts ??= World.Services.TryGet<GameplayMemory>()?.Context("StoryInteraction_Triggered");
        string TriggeredLabel => _triggeredLabel ??= $"{bookmark.GUID}_{bookmark.ChapterName}:triggered";
        bool WasTriggered => TriggeredStoryFacts?.Get<bool>(TriggeredLabel) ?? false;
        
        // === Events
        public static class Events {
            public static readonly Event<Location, StoryInteractionToggleData> StoryInteractionToggled = new(nameof(StoryInteractionToggled));
            public static readonly Event<Location, LookAtChangedData> LocationLookAtChanged = new(nameof(LocationLookAtChanged));
        }

        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            bool disabledByOnlyOnce = triggerOnlyOnce && WasTriggered && !canBePerformedAfterStory;
            if (disabledByOnlyOnce) return false;
            if (_story != null) return false;
            return base.AvailableFor(npc, finder);
        }

        public override void OnInteractionStopped(NpcElement npc, INpcInteraction interaction) {
            base.OnInteractionStopped(npc, interaction);
            if (npc?.HasBeenDiscarded ?? true) {
                return;
            }

            TryStopDialogue();
            
            npc.ParentModel.Trigger(Events.StoryInteractionToggled, StoryInteractionToggleData.Exit(false));
        }
        
        public override void OnTalkStarted(Story story, NpcElement npc, INpcInteraction interaction) {
            if (_story != null && _story != story) {
                _interruptingNpc = npc;
            }
        }
        
        public override void OnEndTalk(NpcElement npc, INpcInteraction interaction) {
            if (_story is { WasInterrupted: false }) {
                bool shouldStopGroupInteractionPart = (_interruptingNpc == null && !keepInInteractionAfterStory) ||
                                                      (_interruptingNpc == npc && !interaction.AllowTalk);
                if (shouldStopGroupInteractionPart && interaction is GroupInteractionPart part) {
                    part.TriggerOnEnd();
                }
            }
        }

        void StartDialogue() {
            _interruptingNpc = null;
            var config = StoryConfig.Base(bookmark, GetDialogueViewType(dialogueViewType));
            for (int i = 0; i < actors.Length; i++) {
                Location location = actors[i].assignedIWithActor.ParentModel;
                config.WithLocation(location);
                var spineRotation = overrideSpineRotation ? spineRotationType : 
                    actors[i].interaction is SimpleInteractionBase simpleInteractionBase ? simpleInteractionBase.SpineRotationType : 
                    SpineRotationType.FullRotation;
                location.Trigger(Events.StoryInteractionToggled, StoryInteractionToggleData.Enter(spineRotation));
            }
            _story = Story.StartStory(config);
            if (focusOverride != null) {
                _story.AddElement(new StoryInteractionFocusOverride(focusOverride));
            }

            if (_story.HasBeenDiscarded) {
                AfterStoryDiscarded().Forget();
            } else {
                _story.ListenTo(Model.Events.BeforeDiscarded, _ => AfterStoryDiscarded().Forget());
            }
        }

        void TryStopDialogue() {
            if (allowDiscardingStory && _story is { HasBeenDiscarded: false, IsEnding: false, InvolveHero: false }) {
                ForceStopDialogue();
            }
        }

        void ForceStopDialogue() {
            SyntaxSugar.Nullify(ref _story).FinishStory(true);
        }

        async UniTaskVoid AfterStoryDiscarded() {
            if (triggerOnlyOnce && _story is { WasInterrupted: false }) {
                TriggeredStoryFacts.Set(TriggeredLabel, true);
            }
            if (await AsyncUtil.DelayFrame(this, 3)) {
                _story = null;
                _interruptingNpc = null;
            }
        }

        void Update() {
            if (triggerOnlyOnce && WasTriggered) {
                return;
            }

            if (_story == null) {
                bool canStart = _targetState is TargetState.Active && _activity is Activity.InState;
                canStart = canStart && (startInCombat || !Hero.Current.IsInCombat());
                canStart = canStart && (allNpcsRequiredToFind || !requireAllNPCsToTriggerStory || actors.All(actor => actor.assignedIWithActor != null && (!actor.NPC?.HasElement<DialogueInvisibility>() ?? true)));
                canStart = canStart && !Hero.Current.HasElement<DialogueInvisibility>();
                if (canStart && DistanceSqFromHeroTo(transform.position) < range * range) {
                    StartDialogue();
                }
            } else {
                var endStoryDistance = stopStoryDistance == HeroRequiredDialogueDistance.Never ? EndStoryDistanceSq : DialogueAttachment.DialogueDistanceSqr(stopStoryDistance);
                if (DistanceSqFromHeroTo(transform.position) > endStoryDistance) {
                    ForceStopDialogue();
                }
            }
            
            static float DistanceSqFromHeroTo(Vector3 to) => Hero.Current.Coords.SquaredDistanceTo(to);
        }

        protected override void OnMainActivityChanged(Activity activity) {
            if (_story == null) {
                return;
            }

            if (activity == Activity.InState && _targetState == TargetState.Active) {
                return;
            }
            
            TryStopDialogue();
        }

        void OnDestroy() {
            if (_story is { HasBeenDiscarded: false, IsEnding: false }) {
                ForceStopDialogue();
            }
        }

        public struct EDITOR_Accessor {
            public ref float Range(ref StoryInteraction data) => ref data.range;
        }

        // === Helpers
        enum DialogueViewType {
            Default = 0,
            Bark = 1
        }
        
        static Type GetDialogueViewType(DialogueViewType viewType) =>
            viewType switch {
                DialogueViewType.Default => typeof(VDialogue),
                DialogueViewType.Bark => typeof(VBark),
                _ => typeof(VDialogue)
            };
    }
}