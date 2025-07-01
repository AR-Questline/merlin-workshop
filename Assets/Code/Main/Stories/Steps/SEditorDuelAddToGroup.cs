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
    [Element("Game/Duel/Duel: Add NPCs to specific group"), NodeSupportsOdin]
    public class SEditorDuelAddToGroup : EditorStep {
        public int groupId;
        public LocationReference locations;
        
        [LabelWidth(130)]
        public bool overrideDuelistSettings;
        [LabelWidth(140)]
        [ShowIf(nameof(overrideDuelistSettings))] public DuelistSettings settings = DuelistSettings.Default;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelAddToGroup {
                groupId = groupId,
                locations = locations,
                overrideDuelistSettings = overrideDuelistSettings,
                settings = settings
            };
        }
    }

    public partial class SDuelAddToGroup : StoryStep {
        public int groupId;
        public LocationReference locations;
        public bool overrideDuelistSettings;
        public DuelistSettings settings = DuelistSettings.Default;
        
        public override StepResult Execute(Story story) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so can't add new participants to it");
                return StepResult.Immediate;
            }

            foreach (var character in GetCharacters(story)) {
                if (overrideDuelistSettings) {
                    duelController.AddDuelist(character, groupId, settings);
                } else {
                    duelController.AddDuelist(character, groupId);
                }
            }
            return StepResult.Immediate;
        }
        
        IEnumerable<ICharacter> GetCharacters(Story api) {
            return locations.MatchingLocations(api).Where(l => l.Character != null).Select(l => l.Character);
        }
    }
}