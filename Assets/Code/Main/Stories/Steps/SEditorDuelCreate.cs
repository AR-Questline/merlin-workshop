using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Create"), NodeSupportsOdin]
    public class SEditorDuelCreate : EditorStep {
        public bool autoStart = true;
        public bool autoEnd = true;
        [LabelWidth(140)] public DuelistSettings defaultDuelistSettings = DuelistSettings.Default;

        [Header("Group 0"), LabelWidth(130)] public bool addHeroToGroup0 = true;
        [LabelWidth(130)] public bool addNpcsToGroup0;
        [ShowIf(nameof(addNpcsToGroup0))] public LocationReference locations0;
        public StoryBookmark callbackOnGroup0Victory;
        [ShowIf(nameof(CallbackSetUp0))] public bool useCallbackLocation0;
        [ShowIf(nameof(ShowLocationsForCallback0))] public LocationReference locationToUseCallback0;
        
        [Header("Group 1")] public LocationReference locations1;
        public StoryBookmark callbackOnGroup1Victory;
        [ShowIf(nameof(CallbackSetUp1))] public bool useCallbackLocation1;
        [ShowIf(nameof(ShowLocationsForCallback1))] public LocationReference locationToUseCallback1;
        
        bool CallbackSetUp0 => callbackOnGroup1Victory is { IsValid: true };
        bool CallbackSetUp1 => callbackOnGroup1Victory is { IsValid: true };
        bool ShowLocationsForCallback0 => CallbackSetUp0 && useCallbackLocation0;
        bool ShowLocationsForCallback1 => CallbackSetUp1 && useCallbackLocation1;

        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelCreate {
                autoStart = autoStart,
                autoEnd = autoEnd,
                defaultDuelistSettings = defaultDuelistSettings,
                addHeroToGroup0 = addHeroToGroup0,
                addNpcsToGroup0 = addNpcsToGroup0,
                locations0 = locations0,
                callbackOnGroup0Victory = callbackOnGroup0Victory,
                useCallbackLocation0 = useCallbackLocation0,
                locationToUseCallback0 = locationToUseCallback0,
                locations1 = locations1,
                callbackOnGroup1Victory = callbackOnGroup1Victory,
                useCallbackLocation1 = useCallbackLocation1,
                locationToUseCallback1 = locationToUseCallback1,
            };
        }
    }

    public partial class SDuelCreate : StoryStep {
        public bool autoStart = true;
        public bool autoEnd = true;
        public bool addHeroToGroup0 = true;
        public bool addNpcsToGroup0;
        
        public DuelistSettings defaultDuelistSettings = DuelistSettings.Default;

        public LocationReference locations0;
        public StoryBookmark callbackOnGroup0Victory;
        public bool useCallbackLocation0;
        public LocationReference locationToUseCallback0;
        
        public LocationReference locations1;
        public StoryBookmark callbackOnGroup1Victory;
        public bool useCallbackLocation1;
        public LocationReference locationToUseCallback1;
        
        public override StepResult Execute(Story story) {
            if (World.Any<DuelController>()) {
                if (autoStart) {
                    Log.Important?.Error("Duel already in progress, can't start a new one");
                    return StepResult.Immediate;
                } else {
                    throw new Exception("Duel already in progress, can't start a new one. This is a manual controller duel, aborting story flow");
                }
            }
            
            var duelController = new DuelController(autoEnd, defaultDuelistSettings);
            World.Add(duelController);

            var focusLocation0 = useCallbackLocation0 ? locationToUseCallback0.MatchingLocations(story).FirstOrDefault() : null;
            duelController.AddDuelGroup(GetCharactersForGroup0(story), callbackOnGroup0Victory, focusLocation0);
            var focusLocation1 = useCallbackLocation1 ? locationToUseCallback1.MatchingLocations(story).FirstOrDefault() : null;
            duelController.AddDuelGroup(GetCharactersForGroup1(story), callbackOnGroup1Victory, focusLocation1);
            if (autoStart) {
                duelController.StartDuel();
            }
            return StepResult.Immediate;
        }
        
        IEnumerable<ICharacter> GetCharactersForGroup0(Story api) {
            if (addHeroToGroup0) {
                yield return api.Hero;
            }
            if (addNpcsToGroup0) {
                foreach (var character in locations0.MatchingLocations(api).Where(l => l.Character != null).Select(l => l.Character)) {
                    yield return character;
                }
            }
        }
        
        IEnumerable<ICharacter> GetCharactersForGroup1(Story api) {
            return locations1.MatchingLocations(api).Where(l => l.Character != null).Select(l => l.Character);
        }
    }
}