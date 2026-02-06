using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class DialogueAction : AbstractLocationAction, IRefreshedByAttachment<DialogueAttachment> {
        public override ushort TypeForSerialization => SavedModels.DialogueAction;

        public const float MinAngleToTalkStanding = 160;
        const float MinAngleToTalkCrouched = 100;
        readonly List<StoryBookmark> _bookmarksStack = new();

        public bool IsInDialogue => _createdStory is {HasBeenDiscarded: false};
        public StoryBookmark Bookmark => _bookmarksStack[^1];
        public GesturesSerializedWrapper GesturesWrapper { get; private set; }
        [CanBeNull]
        public Transform ViewFocus => _hasViewFocus ? _dialoguePoint : null;
        public float EndStoryDistanceSqr { get; private set; }
        protected override bool DisableInCombat => true;
        protected override InfoFrame ActionFrameInternal => new(_interactLabel, HeroHasRequiredItem());
        TimeQueue TimeQueue => Services.Get<TimeQueue>();
        float MinAngleToTalk => Hero.Current.IsCrouching ? MinAngleToTalkCrouched : MinAngleToTalkStanding;
        
        float SqrDistanceToHero => (Hero.Current.Coords - _dialoguePoint.position).sqrMagnitude;

        string _interactLabel;
        Story _createdStory;
        bool _registeredToTimeQueue;
        bool _hasViewFocus;
        Transform _dialoguePoint;

        [UnityEngine.Scripting.Preserve] public Story CreatedStory => _createdStory;
        
        public new static class Events {
            public static readonly Event<DialogueAction, DialogueAction> DialogueStarted = new(nameof(DialogueStarted));
        }

        public void InitFromAttachment(DialogueAttachment spec, bool isRestored) {
            if (spec.bookmark == null || spec.bookmark.IsValid == false) {
                Log.Minor?.Error("DialogueAttachment without StoryBookmark assigned! " + LogUtils.GetDebugName(this) + " - " + spec.gameObject.name,
                    spec.gameObject);
            }
            PushStoryOverride(spec.bookmark);
            _dialoguePoint = spec.viewFocus;
            _hasViewFocus = _dialoguePoint != null;
            GesturesWrapper = spec.gesturesWrapper;
            EndStoryDistanceSqr = spec.EndStoryDistanceSqr;
            string customLabel = spec.customDialogueLabel.ToString();
            _interactLabel = string.IsNullOrWhiteSpace(customLabel) ? LocTerms.Talk.Translate() : customLabel;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(AfterFullyInitialized, this);
        }

        void AfterFullyInitialized() {
            // --- We can't have conversation with dead npc
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.AfterDeath, _ => Discard(), this);

            if (_hasViewFocus) {
                return;
            }

            if (ParentModel.TryGetElement(out NpcElement npc)) {
                npc.OnCompletelyInitialized(_ => {
                    SetViewFocus(npc.Head, npc.ParentTransform);
                });
            } else {
                ParentModel.OnVisualLoaded(t => {
                    var head = t.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(c => c.CompareTag("Head"));
                    SetViewFocus(head, t);
                });
            }

            void SetViewFocus(Transform viewFocus, Transform fallbackPoint) {
                if (viewFocus != null) {
                    _dialoguePoint = viewFocus;
                    _hasViewFocus = true;
                } else {
                    _dialoguePoint = fallbackPoint;
                    _hasViewFocus = false;
                }
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (hero == null) {
                return base.GetAvailability(null, interactable);
            }
            
            if (interactable is not Location location) {
                return ActionAvailability.Disabled;
            }
            NpcElement npcElement = location.TryGetElement<NpcElement>();
            if (npcElement == null) {
                return base.GetAvailability(hero, interactable);
            }
            
            // If it's an NPC we don't want to show dialogue action at all.
            if (hero.IsInCombat()) {
                return ActionAvailability.Disabled;
            }

            // --- Check NPC availability
            if (npcElement.IsHostileTo(hero) || !npcElement.Interactor.NpcInInteractState || npcElement.HasElement<DialogueInvisibility>()) {
                return ActionAvailability.Disabled;
            }

            var interaction = npcElement.Behaviours.CurrentInteraction;
            float minAngle = interaction?.MinAngleToTalk ?? MinAngleToTalk;
            Transform npcElementTransform = npcElement.ParentTransform;
            float angle = Vector3.Angle(hero.ParentTransform.position - npcElementTransform.position, npcElementTransform.forward);
            if (angle > minAngle) {
                return ActionAvailability.Disabled;
            }
            
            bool idleAllowsTalk = interaction?.AllowDialogueAction ?? true;
            if (!idleAllowsTalk) {
                return ActionAvailability.Disabled;
            }
            
            return base.GetAvailability(hero, interactable);
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            StartDialogue(interactable, Bookmark, true);
        }

        public void StartDialogue(IInteractableWithHero interactable, StoryBookmark bookmark, bool useRangeCheck) {
            if (interactable is Location location) {
                if (!(location.TryGetElement<NpcElement>()?.Behaviours.CurrentInteraction?.AllowTalk ?? true)) {
                    this.ParentModel.TryGetElement<NpcElement>()?.TryGetElement<BarkElement>()?.OnFailedDialogue();
                    return;
                }
            }

            if (!_createdStory?.HasBeenDiscarded ?? false) {
                _createdStory.Discard();
            }
            _createdStory = Story.StartStory(StoryConfig.Interactable(interactable, bookmark, typeof(VDialogue)));
            
            this.Trigger(Events.DialogueStarted, this);
            _registeredToTimeQueue = false;

            if (_createdStory.HasBeenDiscarded) {
                _createdStory = null;
                return;
            }
            _createdStory.ListenTo(Model.Events.AfterDiscarded, ClearDialogue, this);
            if (useRangeCheck && EndStoryDistanceSqr > 0 && _createdStory.InvolveHero == false) {
                TimeQueue.Register(new TimeAction(this, CheckOutOfRange, 1));
                _registeredToTimeQueue = true;
            }
        }

        void ClearDialogue(Model _ = null) {
            if (_registeredToTimeQueue) {
                _registeredToTimeQueue = false;
                TimeQueue.Unregister(ContextID);
            }
            _createdStory = null;
        }

        void CheckOutOfRange() {
            if (HasBeenDiscarded || Hero.Current == null || _createdStory.InvolveHero) return;
            if (_dialoguePoint == null) {
                Log.Important?.Error($"{LogUtils.GetDebugName(ParentModel)} has DialogueAction with no DialoguePoint");
                return;
            }
            if (SqrDistanceToHero <= EndStoryDistanceSqr) return;
            EndDialogue();
        }

        void EndDialogue() {
            _createdStory?.FinishStory(true);
            ClearDialogue();
        }

        public void PushStoryOverride(StoryBookmark storyBookmark) {
            // If already present then remove and add to ensure it is stack access top
            if (_bookmarksStack.Contains(storyBookmark)) {
                _bookmarksStack.Remove(storyBookmark);
            }
            _bookmarksStack.Add(storyBookmark);
        }

        public void RemoveStoryOverride(StoryBookmark storyBookmark) {
            _bookmarksStack.Remove(storyBookmark);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            //EndStoryDistance <= 0 means it should never stop
            //Story shouldn't end if there's still hero involved in it
            if (EndStoryDistanceSqr > 0 && _createdStory is { InvolveHero: false }) {
                _createdStory.FinishStory(true);
            }
            ClearDialogue();
        }
    }
}