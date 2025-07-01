using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DiscardOverDistanceBand : Element<Location>, IRefreshedByAttachment<DiscardOverDistanceBandAttachment> {
        public override ushort TypeForSerialization => SavedModels.DiscardOverDistanceBand;

        uint _band;
        
        public void InitFromAttachment(DiscardOverDistanceBandAttachment spec, bool isRestored) {
            _band = spec.Band;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChanged, this);
            OnDistanceBandChanged(ParentModel.GetCurrentBandSafe(0));
        }
        
        void OnDistanceBandChanged(int band) {
            if (band > _band) {
                ParentModel.Discard();
            }
        }
    }
}