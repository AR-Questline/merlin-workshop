using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class TransformAfterChargesSpent : Element<Item>, IRefreshedByAttachment<TransformAfterChargesSpentAttachment>, IItemWithCharges {
        public override ushort TypeForSerialization => SavedModels.TransformAfterChargesSpent;

        [Saved(0)] int _chargesSpent;

        int _chargesToSpend;
        ItemSpawningDataRuntime _transformsInto;
        Action<Item> _afterTransform;
        
        public int ChargesRemaining => _chargesToSpend - _chargesSpent;
        
        public void InitFromAttachment(TransformAfterChargesSpentAttachment spec, bool isRestored) {
            _chargesToSpend = spec.ChargesToSpend;
            _transformsInto = spec.TransformsInto;
            _afterTransform = spec.AfterTransform;
        }
        
        public void SpendCharges(int charges = 1) {
            _chargesSpent += charges;
            if (_chargesSpent >= _chargesToSpend) {
                ParentModel.Trigger(IItemWithCharges.Events.AllChargesSpent, _chargesSpent);
                _chargesSpent = 0;
                Transform().Forget();
            }
        }

        public void RestoreCharges() {
            _chargesSpent = 0;
        }

        async UniTaskVoid Transform() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            ICharacterInventory characterInventory = ParentModel.CharacterInventory;
            var targetSlotToReequip = ParentModel.EquippedInSlotOfType;
            
            if (_transformsInto == null) {
                ParentModel.Discard();
                return;
            }
            
            _transformsInto.elementsData = ParentModel.TryGetRuntimeData();
            ParentModel.Discard();
            
            var resultantItem = characterInventory.AddSingleItem(_transformsInto);
            
            if (targetSlotToReequip != null) {
                characterInventory.Equip(resultantItem, targetSlotToReequip);
            }
            
            _afterTransform.Invoke(resultantItem);
        }
    }
}
