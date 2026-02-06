using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [SpawnsView(typeof(VExpansionUI))]
    public partial class ExpansionUI : Model {
        public override Domain DefaultDomain => Domain.TitleScreen;
        public override bool IsNotSaved => true;

        ExpansionEntryData[] _expansionViews;

        protected override void OnInitialize() {
            _expansionViews = new[] {
                new ExpansionEntryData {
                    expansionIndex = 0,
                    type = LocTerms.Expansion.Translate(),
                    title = LocTerms.ExpansionSanctuaryOfSarras.Translate(),
                    description = LocTerms.ExpansionSanctuaryOfSarrasDesc.Translate(),
                    releaseDate = new DateTime(2025, 12, 15),
                },
                new ExpansionEntryData {
                    expansionIndex = 1,
                    type = LocTerms.ExpansionFree.Translate(),
                    title = LocTerms.ExpansionFreeContentPack.Translate(),
                    description = LocTerms.ExpansionFreeContentPackDesc.Translate(),
                    releaseDate = new DateTime(2025, 1, 1),
                }
            };
        }

        protected override void OnFullyInitialized() {
            var view = View<VExpansionUI>();
            var expansion = AddElement(new ExpansionEntryUI(_expansionViews[0]));
            World.BindView(expansion, view.VSarrasExpansionEntryUI);
            expansion = AddElement(new ExpansionEntryUI(_expansionViews[1]));
            World.BindView(expansion, view.VContentExpansionEntryUI);
        }
    }
}