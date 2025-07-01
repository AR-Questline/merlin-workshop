using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class RemovedAfterChargesSpent : Element<Item>, IRefreshedByAttachment<RemovedAfterChargesSpentAttachment>, IItemWithCharges {
        public override ushort TypeForSerialization => SavedModels.RemovedAfterChargesSpent;

        [Saved(0)] int _chargesSpent;

        int _chargesToSpend;
        
        public void InitFromAttachment(RemovedAfterChargesSpentAttachment spec, bool isRestored) {
            _chargesToSpend = spec.ChargesToSpend;
        }
        public int ChargesRemaining => _chargesToSpend - _chargesSpent;

        public void SpendCharges(int charges = 1) {
            _chargesSpent += charges;
            if (_chargesSpent >= _chargesToSpend) {
                ParentModel.Trigger(IItemWithCharges.Events.AllChargesSpent, _chargesSpent);
                _chargesSpent = 0;
                DelayedDiscard().Forget();
            }
        }

        public void RestoreCharges() {
            _chargesSpent = 0;
        }

        async UniTaskVoid DelayedDiscard() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            ParentModel.Discard();
        }
    }
}