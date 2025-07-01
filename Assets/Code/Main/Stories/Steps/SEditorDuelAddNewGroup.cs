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
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Add new group to Duel"), NodeSupportsOdin]
    public class SEditorDuelAddNewGroup : EditorStep {
        public LocationReference locations;
        public StoryBookmark callbackOnGroupVictory;
        [ShowIf(nameof(CallbackSetUp))] public bool useCallbackLocation;
        [ShowIf(nameof(ShowLocationsForCallback))] public LocationReference locationToUseCallback;
        
        public bool overrideDuelistSettings;
        [ShowIf(nameof(overrideDuelistSettings))] 
        public DuelistSettings settings = DuelistSettings.Default;

        bool CallbackSetUp => callbackOnGroupVictory is { IsValid: true };
        bool ShowLocationsForCallback => CallbackSetUp && useCallbackLocation;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelAddNewGroup {
                locations = locations,
                callbackOnGroupVictory = callbackOnGroupVictory,
                useCallbackLocation = useCallbackLocation,
                locationToUseCallback = locationToUseCallback,
                overrideDuelistSettings = overrideDuelistSettings,
                settings = settings
            };
        }
    }

    public partial class SDuelAddNewGroup : StoryStep {
        public LocationReference locations;
        public StoryBookmark callbackOnGroupVictory;
        public bool useCallbackLocation;
        public LocationReference locationToUseCallback;
        public bool overrideDuelistSettings;
        public DuelistSettings settings = DuelistSettings.Default;
        
        public override StepResult Execute(Story story) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Important?.Error("No duel in progress, so can't add new group to it");
                return StepResult.Immediate;
            }

            var focusLocation = useCallbackLocation ? locationToUseCallback.MatchingLocations(story).FirstOrDefault() : null;
            if (overrideDuelistSettings) {
                duelController.AddDuelGroup(GetCharacters(story), callbackOnGroupVictory, focusLocation, settings);
            } else {
                duelController.AddDuelGroup(GetCharacters(story), callbackOnGroupVictory, focusLocation);
            }
            
            return StepResult.Immediate;
        }
        
        IEnumerable<ICharacter> GetCharacters(Story api) {
            return locations.MatchingLocations(api).Where(l => l.Character != null).Select(l => l.Character);
        }
    }
}