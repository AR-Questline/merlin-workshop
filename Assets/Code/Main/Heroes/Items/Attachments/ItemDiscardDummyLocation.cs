using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemDiscardDummyLocation : ItemOnTakeFromDummyBase, IRefreshedByAttachment<ItemDiscardDummyLocationAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemDiscardDummyLocation;

        bool _dropRemainingItems;

        public void InitFromAttachment(ItemDiscardDummyLocationAttachment spec, bool isRestored) {
            _dropRemainingItems = spec.dropRemainingItems;
        }
        
        protected override void OnTakenFromDummy(NpcDummy dummy) {
            SafeDiscardAfterDelay(dummy.ParentModel).Forget();
        }

        async UniTaskVoid SafeDiscardAfterDelay(Location location) {
            location.SetInteractability(LocationInteractability.Hidden);
            this.Discard();
            if (!await AsyncUtil.DelayFrame(location)) {
                return;
            }
            if (_dropRemainingItems) {
                location.TryGetElement<SearchAction>()?.DropAllItemsAndDiscard(false);
            }
            location.Discard();
        } 
    }
}