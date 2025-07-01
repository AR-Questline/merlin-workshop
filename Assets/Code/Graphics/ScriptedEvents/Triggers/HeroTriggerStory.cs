using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    [RequireComponent(typeof(IHeroTrigger))]
    public class HeroTriggerStory : SceneSpec {
        [SerializeField, BoxGroup("Trigger")] Stage stage;
        [SerializeField, BoxGroup("Trigger")] bool onlyOnce;
        [InfoBox("If empty will try to find a story from the first locationToAdd", visibleIfMemberName: nameof(StoryIsEmpty))]
        [InfoBox("No story or locations to add", InfoMessageType.Error, visibleIfMemberName: nameof(InvalidStory))]
        [SerializeField, BoxGroup("Story"), HideLabel] StoryBookmark story;
        [SerializeField, BoxGroup("Story")] LocationSpec[] locationsToAdd = Array.Empty<LocationSpec>();
        
        bool StoryIsEmpty => story == null || !story.IsValid;
        bool InvalidStory => StoryIsEmpty && (locationsToAdd == null || locationsToAdd.Length == 0);
        
        void Awake() {
            var trigger = GetComponent<IHeroTrigger>();
            if (stage == Stage.OnEnter) {
                trigger.OnHeroEnter += Trigger;
            } else if (stage == Stage.OnExit) {
                trigger.OnHeroExit += Trigger;
            }
        }
        
        void Trigger() {
            if (onlyOnce) {
                bool added = World.Services.Get<SceneSpecCaches>().AddTriggeredSpec(SceneId);
                if (!added) {
                    return;
                }
            }
            
            var selectedStory = SelectStory();
            if (selectedStory == null) {
                return;
            }
            
            var config = StoryConfig.Base(selectedStory, typeof(VDialogue));
            foreach (var spec in locationsToAdd) {
                var location = World.ByID<Location>(spec.GetLocationId()); 
                if (location != null) {
                    location = location.TryGetElement(out NpcPresence presence) 
                        ? presence.AliveNpc?.ParentModel ?? location 
                        : location;
                    config.WithLocation(location);
                }
            }
            Story.StartStory(config);
        }

        StoryBookmark SelectStory() {
            if (story != null && story.IsValid) {
                return story;
            }
            if (locationsToAdd == null || locationsToAdd.Length == 0) {
                return null;
            }
            foreach (var spec in locationsToAdd) {
                var location = World.ByID<Location>(spec.GetLocationId());
                if (TryGetBookmark(location, out var bookmark)) {
                    return bookmark;
                }
                if (location.TryGetElement(out NpcPresence presence) && presence.AliveNpc is { } npc) {
                    if (TryGetBookmark(npc.ParentModel, out bookmark)) {
                        return bookmark;
                    }
                }
            }
            return null;

            static bool TryGetBookmark(Location location, out StoryBookmark bookmark) {
                if (location.TryGetElement(out DialogueAction dialogue)) {
                    bookmark = dialogue.Bookmark;
                    return true;
                }
                bookmark = null;
                return false;
            }
        }

#if UNITY_EDITOR
        [Button]
        void CopyFrom(GameObject other, bool forceCopyName) {
            var otherTransform = other.transform;
            var otherSpec = other.GetComponent<LocationSpec>();
            var otherVariables = other.GetComponent<Variables>();
            var otherCollider = other.GetComponentInChildren<BoxCollider>();
            
            var thisCollider = gameObject.GetComponent<BoxCollider>();
            
            if (!otherSpec || !otherVariables || !otherCollider || !thisCollider) {
                Log.Important?.Error("Cannot copy. GameObject is not a CubeTrigger.");
                return;
            }

            var variableUseBookmark = otherVariables.declarations.Get<bool>("UseBookmark");
            var variableBookmark = otherVariables.declarations.Get<string>("Bookmark");
            var variableOnlyOnce = otherVariables.declarations.Get<bool>("OnlyOnce");
            var variableLocationsToInteract = otherVariables.declarations.Get<List<GameObject>>("NPCToInteract");

            var otherName = other.name;
            if (forceCopyName || !otherName.StartsWith("CubeTriggerDialogue")) {
                gameObject.name = other.name;
            }
            
            otherTransform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            otherCollider.transform.GetLocalPositionAndRotation(out var otherColliderPosition, out var otherColliderRotation);
            localPosition += otherColliderPosition;
            localRotation *= otherColliderRotation;
            transform.SetLocalPositionAndRotation(localPosition, localRotation);

            var otherScale = Vector3Util.cmul(otherTransform.localScale, otherCollider.transform.localScale);
            thisCollider.center = Vector3Util.cmul(otherCollider.center, otherScale);
            thisCollider.size = Vector3Util.cmul(otherCollider.size, otherScale);
            thisCollider.isTrigger = true;
            
            var otherDialogue = variableLocationsToInteract[0].GetComponentInChildren<DialogueAttachment>();
            
            onlyOnce = variableOnlyOnce;
            story = otherDialogue != null
                ? variableUseBookmark
                    ? StoryBookmark.ToSpecificChapter(otherDialogue.bookmark.story, variableBookmark)
                    : otherDialogue.bookmark
                : null;

            var locationsToAddList = new List<LocationSpec>();
            foreach (var go in variableLocationsToInteract) {
                var spec = go.GetComponent<LocationSpec>();
                if (spec == null || spec == otherSpec) {
                    continue;
                }
                locationsToAddList.Add(spec);
            }
            locationsToAdd = locationsToAddList.ToArray();
        }
#endif
        enum Stage : byte {
            OnEnter,
            OnExit,
        }
    }
}