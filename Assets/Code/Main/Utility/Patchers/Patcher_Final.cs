using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    // Final Patcher updates stuff from any to current version
    public class Patcher_Final : Patcher {
        protected override Version MinInputVersion => new(0, 0);
        protected override Version MaxInputVersion => new(9, 9);
        protected override Version FinalVersion => PatcherService.CurrentVersion;

        // Final patcher always operates
        public override bool CanPatch(Version version) {
            return true;
        }

        public override bool AfterDeserializedModel(Model model) {
            return RemoveModelsWithRemovedTemplates(model);
        }

        static bool RemoveModelsWithRemovedTemplates(Model model) {
            return model switch {
                Status status => status.Template != null,
                Item item => item.Template != null,
                Skill skill => skill.Graph != null,
                Quest quest => quest.Template != null,
                Location location => VerifyLocation(location),
                StatTweak tweak => tweak.Owner != null,
                _ => true
            };
        }
        
        static bool VerifyLocation(Location location) {
            return location.ValidAfterUpdate
                   || World.Services.TryGet<SpecSpawner>()?.FindSpecFor(location) != null;
        }
    }
}