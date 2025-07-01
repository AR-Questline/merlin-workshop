using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.Factions {
    public partial class SimpleFactionProvider : Element<Location>, IWithFaction, IRefreshedByAttachment<FactionProviderAttachment> {
        public override ushort TypeForSerialization => SavedModels.SimpleFactionProvider;

        FactionContainer _factionContainer = new();
        public Faction Faction => _factionContainer.Faction;
        public FactionTemplate GetFactionTemplateForSummon() => _factionContainer.GetFactionTemplateForSummon();
        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.OverrideFaction(faction, context);
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.ResetFactionOverride(context);

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public SimpleFactionProvider() { }
        public SimpleFactionProvider(FactionTemplate factionTemplate) {
            _factionContainer.SetDefaultFaction(factionTemplate);
        }
        
        public void InitFromAttachment(FactionProviderAttachment spec, bool isRestored) {
            _factionContainer.SetDefaultFaction(spec.FactionTemplate);
        }
        
        protected override void OnFullyInitialized() {
            if (ParentModel.Elements<IWithFaction>().CountGreaterThan(1)) {
                Log.Important?.Error("There should be only one faction provider per location");
            }
        }
    }
}