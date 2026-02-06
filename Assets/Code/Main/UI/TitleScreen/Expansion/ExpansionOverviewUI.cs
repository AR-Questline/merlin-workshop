using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.UI.TitleScreen.Expansion {
    [SpawnsView(typeof(VExpansionOverviewUI))]
    public class ExpansionOverviewUI : Model {
        public override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.TitleScreen;
        public int InitialCardIndex { get; private set; }

        public ExpansionOverviewUI(int initialCardIndex) {
            InitialCardIndex = initialCardIndex;
        }

        protected override void OnFullyInitialized() {
            var view = View<VExpansionOverviewUI>();
            var expansions = World.All<ExpansionEntryUI>().ToArraySlow();
            
            var expansion = expansions[0];
            view.AddCard(view.VSarrasExpansionCardUI);
            World.BindView(expansion, view.VSarrasExpansionCardUI);
            
            expansion = expansions[1];
            view.AddCard(view.VContentExpansionCardUI);
            World.BindView(expansion, view.VContentExpansionCardUI);
        }
    }
}