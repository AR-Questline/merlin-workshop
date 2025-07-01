using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public abstract class NpcInteractionBase : MonoBehaviour, INpcInteraction {
        protected const string SelectingGroup = "Selecting";
        protected const string HideIfIsUniqueGroup = SelectingGroup + "/HideIfIsUnique";
        protected const string HideIfNotIsUniqueGroup = SelectingGroup + "/HideIfNotIsUnique";
        protected const string DialogueGroup = "Dialogue";
        protected const string InterruptingGroup = "Interrupting";
        protected const string InteractingGroup = "Interacting";
        protected const string PositioningGroup = "Positioning";

        protected const int SelectedGroupOrder = 0;
        protected const int InteractingGroupOrder = 1;
        protected const int InterruptingGroupOrder = 2;
        protected const int DialogueGroupOrder = 3;
        protected const int PositioningGroupOrder = 4;

        const string UniqueIDGroup = HideIfNotIsUniqueGroup + "/UniqueID (only triggered by story)";
        const string AvailabilityGroup = HideIfIsUniqueGroup + "/Availability";
        
        [FoldoutGroup(SelectingGroup, SelectedGroupOrder, Expanded = true), HideIfGroup(HideIfNotIsUniqueGroup, Condition = nameof(Editor_HideUnique)), FoldoutGroup(UniqueIDGroup, Expanded = true)]
        [InfoBox("If set, only story can trigger this interaction.")]
        [SerializeField, Tags(TagsCategory.InteractionID)]
        string uniqueID;

        [FoldoutGroup(SelectingGroup, SelectedGroupOrder, Expanded = true), HideIfGroup(HideIfIsUniqueGroup, Condition = nameof(IsUnique)), FoldoutGroup(AvailabilityGroup, Expanded = true)]
        [SerializeField, InlineProperty, HideLabel]
        [InfoBox("Set tags upon which this interaction will be available. Leave empty to always be available.")]
        FlagLogic availability;

        [SerializeField, FoldoutGroup(DialogueGroup, DialogueGroupOrder, Expanded = true)]
        bool allowBarks = true;
        [SerializeField, FoldoutGroup(DialogueGroup), ShowIf(nameof(allowBarks))]
        bool overrideIdleBarksCooldown;
        [SerializeField, FoldoutGroup(DialogueGroup), ShowIf(nameof(overrideIdleBarksCooldown))] 
        float idleBarksCooldown = 10f;
        
        [Tooltip("This applies to barks and has the following meanings:" +
                 "\nDefault: A random selection from the Idle bark set." +
                 "\nWork: A random bark chosen from the WorkIdle collection." +
                 "\nSleeping: A bark from the SleepingIdle set.")]
        [SerializeField, FoldoutGroup(DialogueGroup)]
        InteractionType interactionType = InteractionType.Default;
        [SerializeField, FoldoutGroup(DialogueGroup)]
        DialogueType dialogueType = DialogueType.Dialogue;
        public event Action OnInternalEnd;
        protected NpcElement _interactingNpc;

        protected virtual bool Editor_HideUnique => false;
        public bool IsUnique => !uniqueID.IsNullOrWhitespace();
        public bool IsAvailable => availability.Get(true);

        public virtual bool CanBeInterrupted => true;
        public virtual bool CanBePushedFrom => CanBeInterrupted;
        public virtual bool AllowBarks => allowBarks;
        public bool OverrideIdleBarksCooldown => overrideIdleBarksCooldown;
        public float IdleBarksCooldown => idleBarksCooldown;
        public bool AllowDialogueAction => dialogueType != DialogueType.Disabled;
        public bool AllowTalk => dialogueType == DialogueType.Dialogue;
        public virtual bool AllowGlancing => AllowTalk;
        public DialogueType SelectedDialogueType => dialogueType;
        public InteractionType SelectedInteractionType => interactionType;
        public virtual float? MinAngleToTalk => null;
        public virtual int Priority => 0;
        public virtual bool FullyEntered => true;
        public virtual bool AllowUseIK => true;
        public virtual bool CanUseTurnMovement => true;
        public virtual GesturesSerializedWrapper Gestures => null;
        
        public abstract Vector3? GetInteractionPosition(NpcElement npc);
        public abstract Vector3 GetInteractionForward(NpcElement npc);

        public void SetUniqueId(string newId) {
            if (!uniqueID.IsNullOrWhitespace()) {
                World.Services.Get<InteractionProvider>().UnregisterUniqueSearchable(uniqueID);
            }
            uniqueID = newId;
            if (!newId.IsNullOrWhitespace()) {
                World.Services.Get<InteractionProvider>().TryRegisterUniqueSearchable(newId, this);
            }
            
            if (_interactingNpc is { HasBeenDiscarded: false }) {
                _interactingNpc.Behaviours.RefreshCurrentBehaviour(true);
            } 
        }

        public virtual bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return IsAvailable && (IsUnique == finder is InteractionUniqueFinder);
        }

        public abstract InteractionBookingResult Book(NpcElement npc);
        public abstract void Unbook(NpcElement npc);
        public abstract void StartInteraction(NpcElement npc, InteractionStartReason reason);
        public abstract void StopInteraction(NpcElement npc, InteractionStopReason reason);
        public abstract void ResumeInteraction(NpcElement npc, InteractionStartReason reason);
        public abstract void PauseInteraction(NpcElement npc, InteractionStopReason reason);
        public virtual bool IsStopping(NpcElement npc) => false;
        
        public virtual bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public virtual void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public virtual bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;

        public void TriggerOnEnd() {
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }

        protected virtual void OnEnable() {
            gameObject.layer = RenderLayers.AIInteractions;
            if (uniqueID.IsNullOrWhitespace()) return;
            World.Services.Get<InteractionProvider>().TryRegisterUniqueSearchable(uniqueID, this);
        }

        protected virtual void OnDisable() {
            if (uniqueID.IsNullOrWhitespace()) return;
            World.Services.Get<InteractionProvider>().UnregisterUniqueSearchable(uniqueID);
        }

#if UNITY_EDITOR
        void Reset() {
            gameObject.layer = RenderLayers.AIInteractions;
        }

        [Button("Set ground position"), FoldoutGroup(PositioningGroup, PositioningGroupOrder)]
        protected void SnapToGround() {
            bool isOpenWorld = false;
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                isOpenWorld |= CommonReferences.Get.SceneConfigs.IsOpenWorld(SceneReference.ByScene(SceneManager.GetSceneAt(i)));
            }
            Vector3 position = isOpenWorld ? Ground.FindClosestNotBelowTerrain(transform.position, transform) : Ground.SnapToGround(transform.position, transform);
            if (math.abs(position.y - transform.position.y) > 0.001f) {
                transform.position = position;
                if (!Application.isPlaying) {
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                }
            }
        }
#endif
    }
    
    public enum DialogueType : byte {
        Dialogue,
        Bark,
        Disabled,
    }

    public enum InteractionType : byte {
        Default,
        Work,
        Sleeping
    }
}