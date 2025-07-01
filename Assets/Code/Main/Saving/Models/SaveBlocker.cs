using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Saving.Models {
    /// <summary>
    /// Marker model - means that World cannot be saved and there is no hope left. 
    /// </summary>
    public partial class SaveBlocker : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public string SourceID { get; private set; }

        public SaveBlocker(string sourceID) {
            SourceID = sourceID;
        }
        public SaveBlocker(IModel sourceModel) {
            SourceID = sourceModel.ContextID;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            Log.Marking?.Warning($"[Blocker] Entering SaveBlocker for {SourceID}. World cannot be saved until this blocker is removed.");
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            Log.Marking?.Warning($"[Blocker] Exiting SaveBlocker for {SourceID}. World can now be saved again if no other blockers are present.");
        }
    }
}