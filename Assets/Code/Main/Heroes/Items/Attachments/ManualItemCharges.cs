using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ManualItemCharges : Element<Item>, IRefreshedByAttachment<ManualItemChargesAttachment>, IItemWithCharges {
        public override ushort TypeForSerialization => SavedModels.ManualItemCharges;

        [Saved(0)] int _chargesSpent;
        int _originalChargeCount;

        public void InitFromAttachment(ManualItemChargesAttachment spec, bool isRestored) {
            _originalChargeCount = spec.ChargesToSpend;
        }
        public int ChargesRemaining => _originalChargeCount - _chargesSpent;
        
        [UnityEngine.Scripting.Preserve]
        public int ChargesSpent {
            get => _chargesSpent;
            set => _chargesSpent = value;
        }

        [UnityEngine.Scripting.Preserve]
        public int OriginalChargeCount {
            get => _originalChargeCount;
            set => _originalChargeCount = value;
        }

        public void SpendCharges(int charges = 1) {
            _chargesSpent += charges;
            if (_chargesSpent >= _originalChargeCount) {
                ParentModel.Trigger(IItemWithCharges.Events.AllChargesSpent, _chargesSpent);
            }
        }

        public void RestoreCharges() {
            _chargesSpent = 0;
        }
    }
}