using System;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher114_115 : Patcher_RestoreOnFastTravelOrSpawn {
        protected override Version MaxInputVersion => new(1, 14, 9999);
        protected override Version FinalVersion => new(1, 15, 0);

        public Patcher114_115() : base(new[] {
            CampaignMapHoS,
            CampaignMapCuanacht,
            CampaignMapForlorn,
        }) { }
            
        public override bool AfterDeserializedModel(Model model) {
            if (model is HeroDevelopment development) {
                development.ListenToLimited(World.Events.ModelInitialized<HeroDevelopment>(), () => {
                    if (!development.HasElement<SarrasHeroTreeBranches>()) {
                        development.AddElement<SarrasHeroTreeBranches>();
                    }
                }, null);
            }
            
            return base.AfterDeserializedModel(model);
        }
    }
}